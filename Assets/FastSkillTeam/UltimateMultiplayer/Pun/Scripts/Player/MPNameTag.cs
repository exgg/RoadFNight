/////////////////////////////////////////////////////////////////////////////////
//
//  MPNameTag.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//  Original code copyright (c) Opsive. https://www.opsive.com
//
//	Description:	A nametag for any object with a renderer. will fade out if
//					obscured by objects or going out of range.
//					
//	NOTE:			OnGUI can be used to draw the nametag, which is convienent
//					but not the most performant. Avoid using it for mobile.
//
/////////////////////////////////////////////////////////////////////////////////


namespace FastSkillTeam.UltimateMultiplayer.Pun.UI
{
	using UnityEngine;
	using Photon.Pun;
	using Opsive.UltimateCharacterController.Game;
	using Opsive.Shared.UI;
	using FastSkillTeam.UltimateMultiplayer.Pun.Utility;

	public class MPNameTag : MonoBehaviourPun
    {
		[Header("GUI")]
		[SerializeField] protected bool m_UseOnGUI = false;
		[Tooltip("The font to draw text with. NOTE: can not be altered at runtime.")]
		[SerializeField] protected Font m_GUIFont;
		[Tooltip("Primitive outline (not for mobile use).")]
		[SerializeField] protected bool m_GUIOutline = false;
		[Tooltip("Primitive dropshadow (not for mobile use).")]
		[SerializeField] protected bool m_GUIDropShadow = true;
		[Tooltip("Outline and dropshadow color.")]
		[SerializeField] protected Color m_OutlineColor = new Color(0, 0, 0, 1); 

		[Header("Unity UI")]
		[SerializeField] protected Text m_Text;
		[SerializeField] protected bool m_ClampHorizontally = true;

		[Header("Shared Settings")]
		[Tooltip("Size of nametag font.")]
		[SerializeField] protected int m_FontSize = 14;
		[SerializeField] protected float m_WorldHeightOffset = 2.25f;
		[SerializeField] protected float m_MaxViewDistance = 1000f;
		[Tooltip("If true, TargetAlpha will attempt to be 1, otherwise 0.")]
		[SerializeField] protected bool m_Visible = true;
		[SerializeField] protected bool m_VisibleThroughObstacles = false;
		[Tooltip("For when going out of range or getting obscured.")]
		[SerializeField] protected float m_FadeOutSpeed = 3.0f;
		[Tooltip("For when arriving within range or being revealed.")]
		[SerializeField] protected float m_FadeInSpeed = 4.0f;
		[SerializeField] protected AutoColoringType m_AutoColoringType = AutoColoringType.TeamColors;
		[SerializeField] protected Color m_DefaultColor = new Color(1, 1, 1, 1);
		[SerializeField] protected Color m_FriendlyColor = new Color(0, 0, 1, 1);
		[SerializeField] protected Color m_EnemyColor = new Color(1, 0, 0, 1);

		private RectTransform m_RectTransform;
		private Color m_Color = new Color(1, 1, 1, 1);
		private float m_Alpha = 0f;
		private float m_TargetAlpha = 1.0f; // opacity that we are fading towards
		private float m_OutlineAlpha = 0.0f;// opacity of the outline (this is kept slightly lower while fading out)
		private string m_NameText = "Unnamed";// name that gets rendered to the screen                     
		private string m_RefreshedText = "";  // used to detect runtime text changes, for refreshing label size
		private float m_Distance = 0.0f; // current distance from camera to target
		private Renderer[] m_Renderers = null; // first renderer on target transform. used to determine if object is visible
		private Vector3 m_ScreenPos = Vector3.zero; // screen space position of nametag
		private Vector2 m_ScreenSize = new Vector2(100, 25); // screen space scale of nametag
		private Rect m_NameRect = new Rect(); // rectangles for GUI drawing
		private Rect m_OutlineRect = new Rect();
		private int m_RefreshedFontSize = 0; // used to detect runtime font changes, for refreshing screen size (useful for development only)
		private GUIStyle m_NameTagStyle = null; // NOTE: don't use this directly. instead, use its property below
		public GUIStyle NameTagStyle // nametag runtime generated GUI style
		{
			get
			{
				if (m_NameTagStyle == null)
				{
					m_NameTagStyle = new GUIStyle("Label");
					m_NameTagStyle.font = m_GUIFont;
					m_NameTagStyle.alignment = TextAnchor.LowerCenter;
					m_NameTagStyle.fontSize = m_FontSize;
				}
				return m_NameTagStyle;
			}
		}

		public enum AutoColoringType { TeamColors, FriendlyOrEnemy, None }
		public string NameText { get => m_NameText; set => m_NameText = value; }
		public float WorldHeightOffset { get => m_WorldHeightOffset; set => m_WorldHeightOffset = value; }  // vertical offset from transform in world coordinates
		public float MaxViewDistance { get => m_MaxViewDistance; set => m_MaxViewDistance = value; } // distance at which nametag will fade away. if zero, view distance is unlimited
		public bool Visible { get => m_Visible; set => m_Visible = value; } // if true, TargetAlpha will attempt to be 1, otherwise 0
		public bool VisibleThroughObstacles { get => m_VisibleThroughObstacles; set => m_VisibleThroughObstacles = value; }     // if true, nametag can be seen through walls
		public Color Color { get => m_Color; set => m_Color = value; } // main text color
		public float Alpha { get => m_Alpha; set => m_Alpha = value; } // current main opacity. this can be set remotely to snap alpha

		public void SetColor(Color color)
		{
			m_Color = color;
		}

		public void SetColor(int teamNumber)
		{
			if (MPTeamManager.Exists)
			{
				if (teamNumber > 0)
				{
					if (m_AutoColoringType == AutoColoringType.TeamColors)
						m_Color = MPTeamManager.Instance.Teams[teamNumber].Color;
					else if (m_AutoColoringType == AutoColoringType.FriendlyOrEnemy)
						m_Color = teamNumber == MPLocalPlayer.Instance.TeamNumber ? m_FriendlyColor : m_EnemyColor;
					else m_Color = m_DefaultColor;
				}
				else m_Color = Color.white;
			}
			else if (m_AutoColoringType == AutoColoringType.FriendlyOrEnemy)
				m_Color = teamNumber == MPLocalPlayer.Instance.TeamNumber ? m_FriendlyColor : m_EnemyColor;
			else m_Color = m_DefaultColor;
		}

		private void Awake()
		{
			if (m_UseOnGUI == true && m_Text.gameObject != null && m_Text.gameObject.activeSelf)
				m_Text.gameObject.SetActive(false);
		}

		/// <summary>
		/// fetches the first renderer in this transform, whose visibility
		/// we'll be checking against
		/// </summary>
		private void Start()
		{
			GameObject character;
			MPPlayer player = GetComponentInParent<MPPlayer>();
			if (player)
				character = player.GameObject;
			else character = transform.root.gameObject;

			m_Renderers = character.GetComponentsInChildren<Renderer>(true);
			PhotonView p = character.GetComponentInChildren<PhotonView>();

			if (p != null)
			{
				m_NameText = MPPlayer.GetName(p.OwnerActorNr); // TODO: don't set directly (?)
				if (m_UseOnGUI == false && m_Text.gameObject != null)
				{
					m_RectTransform = m_Text.gameObject.GetComponent<RectTransform>();
					m_Text.text = m_NameText;
					m_RectTransform.localPosition = new Vector3(0f, m_WorldHeightOffset, 0f);
				}
				return;
			}
			else m_NameText = character.name;

			// SNIPPET: for use showing itempickup IDs
			//var v = transform.root.GetComponentInChildren<Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Objects.PunItemPickup>();
			//if (v != null)
			//{
			//    m_NameText = v.GetComponent<PhotonView>().OwnerActorNr.ToString();
			//    return;
			//}
		}

		/// <summary>
		/// 
		/// </summary>
		private void OnGUI()
		{		
			if (m_UseOnGUI == false)
				return;

			if (!UpdateVisibility())
				return; // abort rendering when object goes off screen

			if (!UpdateFade())
				return; // abort rendering when alpha goes zero

			// we have stuff to draw, yay!

			RefreshText();

			DrawDropShadow();

			DrawOutline();

			DrawText();
		}

		/// <summary>
		/// 
		/// </summary>
		private void Update()
		{	
			if (m_UseOnGUI == true)
				return;

			if (!UpdateVisibility())
				return; // abort rendering when object goes off screen

			if (!UpdateFade())
				return; // abort rendering when alpha goes zero

			// we have stuff to draw, yay!

			RefreshText();

			DrawText();
		}

		/// <summary>
		/// recalculates the screen rectangle for the label and refreshes
		/// font size whenever the text or font size changes
		/// </summary>
		private void RefreshText()
		{
			if ((m_NameText == m_RefreshedText) && (m_FontSize == m_RefreshedFontSize))
				return;

			if (m_RefreshedFontSize != m_FontSize)
				m_NameTagStyle = null;

			m_RefreshedText = m_NameText;
			m_RefreshedFontSize = m_FontSize;
#if TEXTMESH_PRO_PRESENT
			if (m_UseOnGUI == false)
			{
				if (m_Text.TextMeshProText != null)
					m_Text.TextMeshProText.GetPreferredValues();
				return;
			}
#endif
			m_ScreenSize = NameTagStyle.CalcSize(new GUIContent(m_NameText));

			//m_NameTagStyle.normal.textColor = m_Color;	// TODO: use if color breaks
			m_OutlineRect = m_NameRect = new Rect(0, 0, m_ScreenSize.x, m_ScreenSize.y);
		}

		/// <summary>
		/// detects whether the nametag should be drawn (it might be
		/// off-screen, obscured or beyond the view distance) and
		/// swaps the target alpha between 0 and 1 accordingly.
		/// returns false if rendering should be aborted altogether.
		/// </summary>
		private bool UpdateVisibility()
		{
			// NOTE: if object goes off screen we don't fade out but simply kill
			// rendering. however if it goes out of range or gets obscured,
			// we allow rendering while fading out

			m_TargetAlpha = (m_Visible ? 1.0f : 0.0f);

			bool onScreen = false;

			if (MPLocalPlayer.Instance != null)
			{
				for (int i = 0; i < m_Renderers.Length; i++)
				{
					if (m_Renderers[i] == null)
						continue;

					//For a more simple check not requiring a camera this can be used instead of the below.
					//if (m_Renderers[i].isVisible)
					//{
					//	onScreen = true;
					//	break;
					//}

					if (MP3DUtility.OnScreen(Camera.main, m_Renderers[i], transform.position + (Vector3.up * m_WorldHeightOffset), out m_ScreenPos))
					{
						onScreen = true;
						break;
					}
				}
			}

			// check if object is on-screen
			if (onScreen == false)
			{
				m_TargetAlpha = 0.0f;
				return false;   // nothing to render, abort
			}

			// if we have a view distance, check if object is within range
			if ((MaxViewDistance > 0.0f) && !MP3DUtility.WithinRange(Camera.main.transform.position, transform.position, MaxViewDistance, out m_Distance))
				m_TargetAlpha = 0.0f;

			// if this nametag can be obscured, validate line of sight
			if (!VisibleThroughObstacles && !MP3DUtility.InLineOfSight(Camera.main.transform.position, transform,
											(Vector3.up * m_WorldHeightOffset), LayerManager.Default))
				m_TargetAlpha = 0.0f;

			return true;
		}

		/// <summary>
		/// calculates a new alpha value depending on whether we're fading
		/// in or out. NOTE: if this method returns zero it means the
		/// nametag is invisible and should not be rendered
		/// </summary>
		private bool UpdateFade()
		{
			if (m_TargetAlpha > m_Alpha)    // fading in
			{
				m_Alpha = MathUtility.SnapToZero(MathUtility.ReduceDecimals(Mathf.Lerp(m_Alpha, m_TargetAlpha, Time.deltaTime * m_FadeInSpeed)), 0.01f);
				m_OutlineAlpha = m_Alpha;
			}
			else if (m_TargetAlpha < m_Alpha)   // fading out
			{
				m_Alpha = MathUtility.SnapToZero(MathUtility.ReduceDecimals(Mathf.Lerp(m_Alpha, m_TargetAlpha, Time.deltaTime * m_FadeOutSpeed)), 0.01f);

				// because of the rather primitive outline solution, fading out does not look
				// good with outline/dropshadow so we calculate a lower alpha value here
				m_OutlineAlpha = Mathf.Max(0.0f, (m_Alpha - (1.0f - (Mathf.SmoothStep(0, 1, m_Alpha)))));
			}

			if (m_Alpha <= 0.0f)
				return false;

			return true;
		}

		/// <summary>
		/// draws the text
		/// </summary>
		private void DrawText()
		{
			m_Color.a = m_Alpha;

			if (m_UseOnGUI == false)
			{
				if (m_Text.gameObject != null)
				{
					m_Text.text = m_NameText;
					m_Text.SetColor(m_Color);
					if (MPLocalPlayer.Instance)
					{
						Vector3 dir = m_Text.gameObject.transform.position - MPLocalPlayer.Instance.GetUltimateCharacterLocomotion.LookSource.LookPosition(false);
						if (m_ClampHorizontally)
							dir.y = 0;
						Quaternion lookDir = Quaternion.LookRotation(dir);
						m_Text.gameObject.transform.rotation = lookDir;
					}
					float newSize = m_FontSize * m_RectTransform.sizeDelta.magnitude * (m_Distance / m_MaxViewDistance);
					m_Text.fontSize = newSize;
				}

				return;
			}

			GUI.color = m_Color;
			m_NameRect.x = (m_ScreenPos.x - (m_ScreenSize.x * 0.5f));
			m_NameRect.y = (Screen.height - m_ScreenPos.y) - m_FontSize;
			GUI.Label(m_NameRect, m_NameText, NameTagStyle);
			GUI.color = Color.white;

		}

		/// <summary>
		/// this is a brute-force outline solution and nothing for the
		/// faint-hearted ;). should work with no issues on desktop,
		/// but you may want to disable this in mobile projects
		/// </summary>
		private void DrawOutline()
		{
			if (!m_GUIOutline)
				return;

			m_OutlineColor.a = m_OutlineAlpha;
			GUI.color = m_OutlineColor;

			m_OutlineRect.x = m_NameRect.x - 1;
			m_OutlineRect.y = m_NameRect.y;
			GUI.Label(m_OutlineRect, m_NameText, NameTagStyle);

			m_OutlineRect.y = m_NameRect.y - 1;
			GUI.Label(m_OutlineRect, m_NameText, NameTagStyle);

			m_OutlineRect.x = m_NameRect.x;
			GUI.Label(m_OutlineRect, m_NameText, NameTagStyle);

			m_OutlineRect.x = m_NameRect.x + 1;
			GUI.Label(m_OutlineRect, m_NameText, NameTagStyle);

			m_OutlineRect.y = m_NameRect.y;
			GUI.Label(m_OutlineRect, m_NameText, NameTagStyle);

			m_OutlineRect.y = m_NameRect.y + 1;
			GUI.Label(m_OutlineRect, m_NameText, NameTagStyle);

			m_OutlineRect.x = m_NameRect.x;
			GUI.Label(m_OutlineRect, m_NameText, NameTagStyle);

			m_OutlineRect.x = m_NameRect.x - 1;
			GUI.Label(m_OutlineRect, m_NameText, NameTagStyle);
		}

		/// <summary>
		/// draws a drop shadow. should work with no issues on desktop,
		/// but you may want to disable this in mobile projects
		/// </summary>
		private void DrawDropShadow()
		{
			if (!m_GUIDropShadow)
				return;

			m_OutlineColor.a = m_OutlineAlpha * 0.5f;
			GUI.color = m_OutlineColor;

			m_OutlineRect.x = m_NameRect.x + (m_GUIOutline ? 2 : 1);
			m_OutlineRect.y = m_NameRect.y + (m_GUIOutline ? 2 : 1);
			GUI.Label(m_OutlineRect, m_NameText, NameTagStyle);

			m_OutlineRect.x = m_NameRect.x + (m_GUIOutline ? 2 : 1) - 1;
			m_OutlineRect.y = m_NameRect.y + (m_GUIOutline ? 2 : 1);
			GUI.Label(m_OutlineRect, m_NameText, NameTagStyle);
		}
	}
}