/////////////////////////////////////////////////////////////////////////////////
//
//  MPScoreBoard.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//  Original code copyright (c) Opsive. https://www.opsive.com
//
//	Description:	A classic multiplayer scoreboard. Can also be used for
//					debugging purposes (you can enable any player stat in the list).
//
//                  UseImmediateGUI is GUI only. 
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun.UI
{
	using UnityEngine;
	using System.Collections.Generic;
	using Photon.Pun;
	using Opsive.UltimateCharacterController.UI;
	using Opsive.Shared.Game;
	using UnityEngine.UI;
	using Opsive.Shared.Events;
	using Opsive.Shared.UI;
	using FastSkillTeam.Shared.Utility;
	using TextAlignment = Opsive.Shared.UI.TextAlignment;

	public class MPScoreBoard : MonoBehaviourPunCallbacks
	{

		#region Shared
		[Tooltip("While using immediate GUI (imGUI) is great for prototyping, it is not so great for performance, therefore should not be used with mobile applications. If console or PC, don't stress it will cope. " +
			"\nUnity GUI (uGUI) also has been provided as an example to get your game kickstarted.")]
		[SerializeField] private bool m_UseImmediateGUI = false;
		// --- properties ---

		protected static bool m_ShowScore = false;
		public static bool ShowScore
		{
			get
			{
				return m_ShowScore;
			}
			set
			{
                if (m_ShowScore == value)
                    return;

                m_ShowScore = value;

				EventHandler.ExecuteEvent<bool>("ActivateChat", !m_ShowScore);//See MPChatInput.cs

				if (Crosshair != null)
					m_Crosshair.Visible = !m_ShowScore;
			}
		}

		protected static CrosshairsMonitor m_Crosshair = null;
		protected static CrosshairsMonitor Crosshair
		{
			get
			{
				if (m_Crosshair == null)
					m_Crosshair = Component.FindObjectOfType<CrosshairsMonitor>();
				return m_Crosshair;
			}
		}

		protected bool m_StatNamesChecked = false;
		// NOTE: all of the stats in the 'VisibleStatNames' list must be present in
		// 'MPLocalPlayer.Instance.Stats' except 'Ping' which is stored in Photon's
		// custom player prefs (as opposed to the UCCMP player state)
		[SerializeField] protected List<string> m_VisibleStatNames = new List<string>(new string[] { "Ping", "Score", "Frags", "Deaths", "Shots" });
		protected List<string> VisibleStatNames
		{
			get
			{
				if (!m_StatNamesChecked)
				{
					if (m_VisibleStatNames == null)
						m_VisibleStatNames = new List<string>();

					if (m_VisibleStatNames.Count > 0)
					{
						if (MPLocalPlayer.Instance != null)
						{
							for (int v = m_VisibleStatNames.Count - 1; v > -1; v--)
							{
								if ((m_VisibleStatNames[v] != "Ping") && !MPLocalPlayer.Instance.Stats.Names.Contains(m_VisibleStatNames[v]))
									m_VisibleStatNames.Remove(m_VisibleStatNames[v]);
							}
						}
					}
					m_StatNamesChecked = true;
				}
				return m_VisibleStatNames;
			}
		}
		public static bool UseScoreBoard;
		private void Update()
		{
			if (UseScoreBoard == false)
				return;

			UpdateUGUI();

			UpdateInput();
		}

		/// <summary>
		/// 
		/// </summary>
		private void UpdateInput()
		{
			if (!m_UseImmediateGUI && m_ShowScore)
			{
				if (!m_ScoreBoardBuilt)
				{
					BuildScoreBoard(true);
				}
			}
			else
			{
				if (m_ScoreBoardBuilt)
				{
					BuildScoreBoard(false);
				}
			}

			if (m_NextRefreshTime > 0)
			{
				if (Time.time > m_NextRefreshTime)
				{
					if (m_ScoreBoardBuilt)
						BuildScoreBoard(true);
					//	Debug.Log("Refresh");
				}
			}

			if (MPMaster.Phase != MPMaster.GamePhase.Playing)
				return;

#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
            ShowScore = Keyboard.current.tabKey.wasPressedThisFrame;
#else
            ShowScore = (Input.GetKey(KeyCode.Tab));
#endif
        }

        /// <summary>
        /// TODO: move to timeutility?
        /// </summary>
        string GetFormattedTime(float t)
		{

			t = Mathf.Max(0, t);
			int hours = ((int)t) / 3600;
			int minutes = (((int)t) - (hours * 3600)) / 60;
			int seconds = ((int)t) % 60;
			string s = "";
			s += (hours > 0) ? hours.ToString() + ":" : "";
			s += ((minutes > 0) ? (minutes < 10) ? "0" + minutes + ":" : minutes + ":" : "");
			s += (seconds < 10) ? "0" + seconds : seconds.ToString();
			return s;

		}


		/// <summary>
		/// 
		/// </summary>
		public override void OnJoinedRoom()
		{
			base.OnJoinedRoom();
			Scheduler.Schedule(0.99f, delegate ()
			{
				MPDebug.Log("Press TAB for SCOREBOARD");
			});
		}
		#endregion

		#region uGUI implementation
		//UGUI Implementation
		[Tooltip("The delay between refreshing data whilst the score board is open.")]
		[SerializeField] private float m_RefreshRate = 2f;

		[Tooltip("The required grid layout group (will try get from this gameobject if null).")]
		[SerializeField] protected GridLayoutGroup m_GridLayoutGroup = null;
		[Tooltip("The parent for the team containers (will use this tranform if null).")]
		[SerializeField] protected Transform m_TeamsParent;
		[Tooltip("The prefab that will be spawned to be populated with the following prefabs for each team.")]
		[SerializeField] protected GameObject m_TeamContainerPrefab;
		[Tooltip("The prefab to act as a title row.")]
		[SerializeField] protected GameObject m_TitleRowPrefab;
		[Tooltip("The prefab to act as a spacer between header and title (Optional).")]
		[SerializeField] protected GameObject m_TitleSpacerPrefab;
		[Tooltip("The prefab to act as a header row.")]
		[SerializeField] protected GameObject m_HeaderRowPrefab;
		[Tooltip("The prefab to act as a player info row.")]
		[SerializeField] protected GameObject m_RowPrefab;
		[Tooltip("The prefab to act as a cell.")]
		[SerializeField] protected GameObject m_CellPrefab;
		[Tooltip("The prefab containg the text component.")]
		[SerializeField] protected GameObject m_LabelPrefab;

		[Tooltip("Rows alternate colors for clarity, you can set the color scheme here.")]
		[SerializeField] protected Color m_RowColourA = new Color(1f, 1f, 1f, 0.5f);
		[Tooltip("Rows alternate colors for clarity, you can set the color scheme here.")]
		[SerializeField] protected Color m_RowColourB = new Color(0.5f, 0.5f, 0.5f, 0.5f);

		/*[SerializeField] protected*/
		string m_OrderBy = "Score";//TODO: complete this implementation after ALL else is done. Its not important, just a feature for later.

		protected GameObject m_GameObject;
		protected Transform m_Transform;

		public GameObject GameObject => m_GameObject;
		public Transform Transform => m_Transform;


		private bool m_ColorSwap = false;
		private bool m_ScoreBoardBuilt = false;
		private float m_NextRefreshTime = 0;
		private int m_OriginalSortingOrder = 0;

		//Lazy init as may not be uend while using debug imGUI (ImmediateGUI)
		private Canvas m_Canvas = null;
		public Canvas GetCanvas { get { if (m_Canvas == null) { m_Canvas = GetComponentInParent<Canvas>(); m_OriginalSortingOrder = m_Canvas.sortingOrder; } return m_Canvas; } }

		private RectTransform m_CanvasRectTransform = null;
		public RectTransform GetCanvasRectTransform { get { if (m_CanvasRectTransform == null && GetCanvas != null) { m_CanvasRectTransform = m_Canvas.GetComponent<RectTransform>(); } return m_CanvasRectTransform; } }

		private void Awake()
		{
			m_Instance = this;
			m_GameObject = gameObject;
			m_Transform = transform;
			if (m_TeamsParent == null)
				m_TeamsParent = m_Transform;
			if (m_GridLayoutGroup == null)
				m_GridLayoutGroup = m_GameObject.GetCachedComponent<GridLayoutGroup>();
		}

		private void UpdateUGUI()
		{
			if (m_UseImmediateGUI)
				return;

			//UGUI Implementation
			int teamCount = m_TeamsParent.childCount;

			if (teamCount < 1)
				teamCount = 1;

			// Calculate the number of columns and rows based on the team count
			int columnCount = Mathf.CeilToInt(Mathf.Sqrt(teamCount));
			int rowCount = Mathf.CeilToInt((float)teamCount / columnCount);

			if (m_GridLayoutGroup)
			{   // Set the column and row counts for the GridLayoutGroup
				m_GridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
				m_GridLayoutGroup.constraintCount = columnCount;
				m_GridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedRowCount;
				m_GridLayoutGroup.constraintCount = rowCount;
				if (GetCanvasRectTransform != null)
					m_GridLayoutGroup.cellSize = new Vector3(m_CanvasRectTransform.sizeDelta.x / columnCount, m_CanvasRectTransform.sizeDelta.y / rowCount, 1f);
			}
		}

		private static MPScoreBoard m_Instance;
		public static void Refresh()
		{
			if (m_Instance == null)
				return;
			if (m_ShowScore == true)
				m_Instance.BuildScoreBoard(true);
		}

		private void OnDestroy()
		{
			m_Instance = null;
		}

		private void BuildScoreBoard(bool build)
		{
			if (GetCanvas == null)
				return;

			if (m_ScoreBoardBuilt)
			{
				for (int i = 0; i < m_TeamsParent.childCount; i++)
				{
					//NOTE: Pool kills UI atm, so we don't use it.

					/*if (ObjectPoolBase.InstantiatedWithPool(m_TeamsParent.GetChild(i).gameObject))
                        ObjectPoolBase.Destroy(m_TeamsParent.GetChild(i).gameObject);
                    else*/
					Destroy(m_TeamsParent.GetChild(i).gameObject);
				}
			}

			if (!build)
			{
				m_Canvas.sortingOrder = m_OriginalSortingOrder;
				m_NextRefreshTime = 0;
				m_ScoreBoardBuilt = false;
				return;
			}

			if (MPTeamManager.Exists && MPTeamManager.TeamCount > 1)
			{
				foreach (MPTeam t in MPTeamManager.Instance.Teams)
				{
					if (t.Number > 0)
					{
						DrawTeamUGUI(t);
					}
				}
			}
			else
			{
				DrawTeamUGUI((MPTeamManager.Exists ? MPTeamManager.Instance.Teams[0] : null));
			}

			m_Canvas.sortingOrder = 99;
			m_NextRefreshTime = Time.time + m_RefreshRate;
			m_ScoreBoardBuilt = true;
		}

		private void DrawTeamUGUI(MPTeam team)
		{
			m_ColorSwap = false;

			Color col = (team != null ? team.Color * (Color.white * 0.35f) : Color.white);
			col.a = 1f;

			//create team container
			GameObject teamPanel = Instantiate(m_TeamContainerPrefab, m_TeamsParent.gameObject.GetCachedComponent<RectTransform>());
			RectTransform contentTransform = teamPanel.GetCachedComponent<RectTransform>();

			//NOTE: Pool kills UI atm, so we don't use it.

			/*if (m_TeamsParent.childCount > 0)
            {
                for (int i = 0; i < m_TeamsParent.childCount; i++)
                {
					if (ObjectPoolBase.InstantiatedWithPool(m_TeamsParent.GetChild(i).gameObject))
						ObjectPoolBase.Destroy(m_TeamsParent.GetChild(i).gameObject);
					else
						Destroy(m_TeamsParent.GetChild(i).gameObject);
				}
            }*/


			int childOffset = 0;
			GameObject cell;
			GameObject labelGameObject;
			TextComponent label;
			if ((team != null) && MPTeamManager.Exists && MPTeamManager.TeamCount > 1)
			{
				/*if (team.Number.IsOdd())
				{
					TeamNameStyle.alignment = TextAnchor.MiddleLeft;
					TeamScoreStyle.alignment = TextAnchor.MiddleRight;
				}
				else
				{
					TeamNameStyle.alignment = TextAnchor.MiddleRight;
					TeamScoreStyle.alignment = TextAnchor.MiddleLeft;
				}*/

				// Create title row
				GameObject titleRow = Instantiate(m_TitleRowPrefab, contentTransform);
				titleRow.GetCachedComponent<Image>().color = col;

				/*if (titleRow.transform.childCount > 0)
				{
					for (int i = 0; i < titleRow.transform.childCount; i++)
					{
						if (ObjectPoolBase.InstantiatedWithPool(titleRow.transform.GetChild(i).gameObject))
							ObjectPoolBase.Destroy(titleRow.transform.GetChild(i).gameObject);
						else
							Destroy(titleRow.transform.GetChild(i).gameObject);
					}
				}*/

				//create timer info
				cell = Instantiate(m_CellPrefab, titleRow.transform);
				labelGameObject = Instantiate(m_LabelPrefab, cell.transform);
				label = labelGameObject.GetCachedComponent<TextComponent>();
				string s = ((MPMaster.Phase == MPMaster.GamePhase.Playing) ?
				"Time Left: " :
				"Next game starts in: ");
				label.text = s + GetFormattedTime(MPClock.TimeLeft);

				label.alignment = TextAlignment.MiddleCenter;


				//create team name
				cell = Instantiate(m_CellPrefab, titleRow.transform);
				labelGameObject = Instantiate(m_LabelPrefab, cell.transform);
				label = labelGameObject.GetCachedComponent<TextComponent>();
				label.text = team.Name.ToUpper();

				label.alignment = TextAlignment.MiddleCenter;

				if (team is MPDMTeam)
				{
					//draw score label
					cell = Instantiate(m_CellPrefab, titleRow.transform);
					labelGameObject = Instantiate(m_LabelPrefab, cell.transform);
					label = labelGameObject.GetCachedComponent<TextComponent>();
					label.text = "Score : " + (team as MPDMTeam).Score.ToString();

					label.alignment = TextAlignment.MiddleCenter;
				}

				childOffset++;
				if (m_TitleSpacerPrefab)
				{
					childOffset++;
					Instantiate(m_TitleSpacerPrefab, contentTransform);
				}

			}

			// Create header row
			GameObject headerRow = Instantiate(m_HeaderRowPrefab, contentTransform);
			for (int i = 0; i < m_VisibleStatNames.Count + 1; i++)
			{
				cell = Instantiate(m_CellPrefab, headerRow.transform);
				labelGameObject = Instantiate(m_LabelPrefab, cell.transform);
				label = labelGameObject.GetCachedComponent<TextComponent>();
				label.text = i == 0 ? "Player" : m_VisibleStatNames[i - 1];

				label.alignment = TextAlignment.MiddleCenter;
			}

			childOffset++;
			int greatestScore = 0;
			foreach (MPPlayer p in MPPlayer.Players.Values)
			{
				if (p == null)
					continue;
				if ((team == null) || p.TeamNumber == team.Number)
				{
					//DrawPlayerRow
					GameObject row = Instantiate(m_RowPrefab, contentTransform);

					Color playerColor = (m_ColorSwap = !m_ColorSwap) ? m_RowColourB : m_RowColourA;
					int score = 0;
					for (int j = 0; j < m_VisibleStatNames.Count + 1; j++)
					{
						cell = Instantiate(m_CellPrefab, row.transform);

						labelGameObject = Instantiate(m_LabelPrefab, cell.transform);
						label = labelGameObject.GetCachedComponent<TextComponent>();

						label.alignment = TextAlignment.MiddleCenter;

						if (j == 0)
						{
							label.text = MPPlayer.GetName(p.ID);
							if (p.photonView.Owner == PhotonNetwork.LocalPlayer)
								playerColor = Color.cyan;
						}
						else
						{
							if (m_VisibleStatNames[j - 1] == "Ping")
							{
								label.text = p.Ping.ToString();
								continue;
							}

							object statValue = p.Stats.Get(m_VisibleStatNames[j - 1]);
							label.text = statValue != null ? statValue.ToString() : "";
							if (m_OrderBy == m_VisibleStatNames[j - 1])
							{
								score = (int)statValue;
							}
						}
					}

					if (score >= greatestScore)
					{
						greatestScore = score;
						row.transform.SetSiblingIndex(childOffset);
					}
					row.GetCachedComponent<Image>().color = playerColor;
				}
			}

		}
		#endregion

		#region imGUI implementation

		public Font Font;                           // NOTE: can not be altered at runtime
		public int TextFontSize = 14;
		public int CaptionFontSize = 25;
		public int TeamNameFontSize = 35;
		public int TeamScoreFontSize = 50;
		public Texture Background = null;

		protected float m_NameColumnWidth = 150;
		protected float m_Margin = 20;
		protected float m_Padding = 10;

		protected Rect labelRect = new Rect(0, 0, 0, 0);
		protected Rect shadowRect = new Rect(0, 0, 0, 0);
		protected Rect m_BGRect;

		protected Vector2 m_Pos = Vector2.zero;
		protected Vector2 m_Size = Vector2.zero;

		protected Color m_CaptionBGColor = new Color(0, 0, 0, 0.5f);
		protected Color m_TranspBlack = new Color(0, 0, 0, 0.5f);
		protected Color m_TranspWhite = new Color(1, 1, 1, 0.5f);
		protected Color m_TranspCyan = new Color(0, 0.8f, 0.8f, 0.5f);
		protected Color m_TranspBlackLine = new Color(0, 0, 0, 0.15f);
		protected Color m_TranspWhiteLine = new Color(1, 1, 1, 0.15f);
		protected Color m_CurrentRowColor;

		protected enum CaptionScoreSetting
		{
			None,
			Left,
			Right
		}


		/// <summary>
		/// 
		/// </summary>
		private float GetColumnWidth(float tableWidth)
		{
			return ((tableWidth - m_NameColumnWidth) / VisibleStatNames.Count);
		}


		/// <summary>
		/// 
		/// </summary>
		private void DrawLabel(string text, Vector2 position, Vector2 scale, GUIStyle textStyle, Color textColor, Color bgColor, bool dropShadow = false)
		{

			if (scale.x == 0)
				scale.x = textStyle.CalcSize(new GUIContent(text)).x;
			if (scale.y == 0)
				scale.y = textStyle.CalcSize(new GUIContent(text)).y;

			labelRect.x = m_Pos.x = position.x;
			labelRect.y = m_Pos.y = position.y;
			labelRect.width = m_Size.x = scale.x;
			labelRect.height = m_Size.y = scale.y;

			if (bgColor != Color.clear)
			{
				GUI.color = bgColor;
				GUI.DrawTexture(labelRect, Background);
			}


			if (dropShadow)
			{
				GUI.color = Color.Lerp(bgColor, Color.black, 0.5f);
				shadowRect = labelRect;
				shadowRect.x += scale.y * 0.1f;
				shadowRect.y += scale.y * 0.1f;
				GUI.Label(shadowRect, text, textStyle);
			}

			GUI.color = textColor;
			GUI.Label(labelRect, text, textStyle);
			GUI.color = Color.white;

			m_Pos.x += m_Size.x;
			m_Pos.y += m_Size.y;

		}


		/// <summary>
		/// 
		/// </summary>
		private void DrawLabel(string text, Vector2 pos)
		{
			DrawLabel(text, pos, Vector2.zero, TextStyle, Color.white, Color.clear);
		}


		/// <summary>
		/// 
		/// </summary>
		private void DrawTeam(Vector2 position, Vector2 scale, MPTeam team)
		{

			Color col = (team != null ? team.Color * (Color.white * 0.35f) : Color.white);
			col.a = 0.75f;

			if ((team != null) && MPTeamManager.Exists && MPTeamManager.TeamCount > 1)
			{
				if (team.Number.IsOdd())
				{
					TeamNameStyle.alignment = TextAnchor.MiddleLeft;
					TeamScoreStyle.alignment = TextAnchor.MiddleRight;
				}
				else
				{
					TeamNameStyle.alignment = TextAnchor.MiddleRight;
					TeamScoreStyle.alignment = TextAnchor.MiddleLeft;
				}
				DrawLabel(team.Name.ToUpper(), position, scale, TeamNameStyle, Color.white, col, true);
				if (team is MPDMTeam)
					DrawLabel((team as MPDMTeam).Score.ToString(), position, scale, TeamScoreStyle, Color.white, Color.clear, true);
				scale.y = m_Size.y;
				m_Pos.y -= m_Padding;
			}

			m_Pos.x = position.x;
			m_Size.y = Screen.height - m_Pos.y - m_Margin;
			DrawLabel("", m_Pos, m_Size, TextStyle, Color.clear, m_TranspBlack);
			m_Pos.y = position.y + scale.y;
			DrawTopRow(new Vector2(position.x, m_Pos.y), scale);
			m_CurrentRowColor = m_TranspBlack;

			foreach (MPPlayer p in MPPlayer.Players.Values)
			{
				if (p == null)
					continue;
				if ((team == null) || p.TeamNumber == team.Number)
				{
					DrawPlayerRow(p, m_Pos + (Vector2.up * m_Size.y), new Vector2(scale.x, m_Size.y));
				}
			}

		}


		/// <summary>
		/// 
		/// </summary>
		private void DrawTopRow(Vector2 position, Vector2 scale)
		{

			m_Pos = position;
			m_Pos.x += scale.x - m_Padding - GetColumnWidth(scale.x);

			foreach (string s in VisibleStatNames)
			{
				DrawStatLabel(s, m_Pos, scale);
			}

			DrawStatLabel("Name", position + (Vector2.right * m_Padding), scale);

			m_Pos.x = position.x;
			m_Size.y = 20;

		}


		/// <summary>
		/// 
		/// </summary>
		private void DrawPlayerRow(MPPlayer p, Vector2 position, Vector2 scale)
		{

			m_CurrentRowColor = ((m_CurrentRowColor == m_TranspWhiteLine) ? m_TranspBlackLine : m_TranspWhiteLine);
			if (p.photonView.Owner == PhotonNetwork.LocalPlayer)
				m_CurrentRowColor = m_TranspCyan;
			DrawLabel(MPPlayer.GetName(p.photonView.OwnerActorNr), position, scale, PlayerTextStyle, Color.white, m_CurrentRowColor);

			m_Pos = position;
			m_Pos.x += scale.x - m_Padding - GetColumnWidth(scale.x);

			foreach (string s in VisibleStatNames)
			{

				if (s == "Ping")
				{
					DrawStatLabel(p.Ping.ToString(), m_Pos, scale);
					continue;
				}

				object stat = p.Stats.Get(s);
				string statOut = stat.ToString();
				DrawStatLabel(statOut.ToString(), m_Pos, scale);

			}


			m_Pos.x = position.x;
			m_Size.y = 20;

		}


		/// <summary>
		/// 
		/// </summary>
		private void DrawStatLabel(string statName, Vector2 position, Vector2 scale)
		{

			DrawLabel(statName, position);
			m_Pos = position;
			m_Pos.x -= GetColumnWidth(scale.x);

		}


		/// <summary>
		/// NOTE: scoreboard does not access game state for data, but
		/// instead fetches it from MPPlayer properties
		/// </summary>
		private void OnGUI()
		{

			if (!m_UseImmediateGUI)
				return;

			if (!ShowScore)
				return;

			m_Pos = Vector2.zero;
			m_Size = Vector2.zero;

			DrawCaption();

			DrawTeams();

		}


		/// <summary>
		/// 
		/// </summary>
		private void DrawTeams()
		{

			Vector2 tPos = new Vector2(m_Margin, m_Pos.y);
			if (Screen.width > 1200)
				tPos.x += (Screen.width - 1200) * 0.5f;
			Vector2 tSize = m_Size;

			if (MPTeamManager.Exists && MPTeamManager.TeamCount > 1)
			{
				foreach (MPTeam t in MPTeamManager.Instance.Teams)
				{
					m_Size.x =
						((Mathf.Min(Screen.width, 1200)) - (m_Margin * 2))
						/ (MPTeamManager.TeamCount - (MPTeamManager.TeamCount < 3 ? 0 : 1))
						- (MPTeamManager.TeamCount < 3 ? 0 : (m_Padding * 0.5f))
						;
					m_Size.y = 0;
					tSize = m_Size;
					if (t.Number > 0)
					{
						DrawTeam(tPos, tSize, t);
						tPos += tSize + ((Vector2.right * (m_Padding * 0.5f)) * 2);
					}
				}
			}
			else
			{
				m_Size.x = ((Mathf.Min(Screen.width, 1200)) - (m_Margin * 2));
				m_Size.y = 0;
				tSize = m_Size;
				DrawTeam(tPos, tSize, (MPTeamManager.Exists ? MPTeamManager.Instance.Teams[0] : null));
				tPos += tSize + ((Vector2.right * (m_Padding * 0.5f)) * 2);
			}
		}


		/// <summary>
		/// 
		/// </summary>
		private void DrawTime()
		{
			float bx = m_Pos.x;
			string s = ((MPMaster.Phase == MPMaster.GamePhase.Playing) ?
				"Time Left: " :
				"Next game starts in: ");
			m_Pos.x = 630 - TextStyle.CalcSize(new GUIContent(s)).x;
			DrawLabel(s + GetFormattedTime(MPClock.TimeLeft)
			//+ " / " + GetFormattedTime(MPMaster.Instance.RoundDuration)			// SNIPPET: uncomment to also show total game duration
			, m_Pos);
			m_Pos.y += 30;
			m_Pos.x = bx;
		}


		/// <summary>
		/// 
		/// </summary>
		private void DrawPlayers()
		{

			DrawLabel("Player", m_Pos);

			m_Pos.x = 200;

			foreach (string s in VisibleStatNames)
			{
				DrawLabel(s, m_Pos);
				m_Pos.x += 100;
			}

			m_Pos.x = 200;
			m_Pos.y += 30;

			foreach (int playerID in MPPlayer.IDs)   // TODO: must be sorted (see old property above)
			{

				DrawLabel(MPPlayer.GetName(playerID), new Vector2(100, m_Pos.y));

				foreach (string s in VisibleStatNames)
				{

					object stat = MPPlayer.Get(playerID).Stats.Get(s);
					string statOut = stat.ToString();


					DrawLabel(statOut.ToString(), m_Pos);
					m_Pos.x += 100;
				}
				m_Pos.x = 200;
				m_Pos.y += 20;
			}

		}


		/// <summary>
		/// 
		/// </summary>
		private void DrawCaption()
		{

			m_Pos.y = m_Margin;
			m_Size.y = 0;

			if (MPMaster.Phase == MPMaster.GamePhase.BetweenGames)
			{
				m_Pos.x = m_Margin;
				if (Screen.width > 1200)
					m_Pos.x += (Screen.width - 1200) * 0.5f;
				m_Size.x = 250;
				DrawLabel("GAME OVER", m_Pos, m_Size, CaptionStyle, Color.yellow, m_CaptionBGColor, true);
				m_Pos.x = m_Margin;
				if (Screen.width > 1200)
					m_Pos.x += (Screen.width - 1200) * 0.5f;
				m_Size.x = 250;
				DrawLabel("Next game in: " + GetFormattedTime(MPClock.TimeLeft), m_Pos, m_Size, CenteredTextStyle, Color.white, m_CaptionBGColor);
			}
			else
			{
				m_Pos.x = m_Margin;
				if (Screen.width > 1200)
					m_Pos.x += (Screen.width - 1200) * 0.5f;
				m_Size.x = 100;
				DrawLabel(GetFormattedTime(MPClock.TimeLeft), m_Pos, m_Size, CaptionStyle, Color.white, m_CaptionBGColor, true);
			}

			m_Pos.y += m_Margin;

		}

		// -------- GUI styles --------

		public GUIStyle TextStyle
		{
			get
			{
				if (m_TextStyle == null)
				{
					m_TextStyle = new GUIStyle("Label");
					m_TextStyle.font = Font;
					m_TextStyle.alignment = TextAnchor.UpperLeft;
					m_TextStyle.fontSize = TextFontSize;
					m_TextStyle.wordWrap = false;
				}
				return m_TextStyle;
			}
		}
		protected GUIStyle m_TextStyle = null;

		public GUIStyle PlayerTextStyle
		{
			get
			{
				if (m_PlayerTextStyle == null)
				{
					m_PlayerTextStyle = new GUIStyle("Label");
					m_PlayerTextStyle.font = Font;
					m_PlayerTextStyle.alignment = TextAnchor.MiddleLeft;
					m_PlayerTextStyle.fontSize = TextFontSize;
					m_PlayerTextStyle.wordWrap = false;
					m_PlayerTextStyle.padding.left = 5;
				}
				return m_PlayerTextStyle;
			}
		}
		protected GUIStyle m_PlayerTextStyle = null;

		public GUIStyle CenteredTextStyle
		{
			get
			{
				if (m_CenteredTextStyle == null)
				{
					m_CenteredTextStyle = new GUIStyle("Label");
					m_CenteredTextStyle.font = Font;
					m_CenteredTextStyle.fontSize = TextFontSize;
					m_CenteredTextStyle.wordWrap = false;
					m_CenteredTextStyle.alignment = TextAnchor.MiddleCenter;
				}
				return m_CenteredTextStyle;
			}
		}
		protected GUIStyle m_CenteredTextStyle = null;

		public GUIStyle CaptionStyle
		{
			get
			{
				if (m_CaptionStyle == null)
				{
					m_CaptionStyle = new GUIStyle("Label");
					m_CaptionStyle.font = Font;
					m_CaptionStyle.alignment = TextAnchor.MiddleCenter;
					m_CaptionStyle.fontSize = CaptionFontSize;
					m_CaptionStyle.wordWrap = false;
					m_CaptionStyle.padding.top = 10;
				}
				return m_CaptionStyle;
			}
		}
		protected GUIStyle m_CaptionStyle = null;

		public GUIStyle TeamNameStyle
		{
			get
			{
				if (m_TeamNameStyle == null)
				{
					m_TeamNameStyle = new GUIStyle("Label");
					m_TeamNameStyle.font = Font;
					m_TeamNameStyle.alignment = TextAnchor.MiddleLeft;
					m_TeamNameStyle.fontSize = TeamNameFontSize;
					m_TeamNameStyle.wordWrap = false;
					m_TeamNameStyle.padding.left = 10;
					m_TeamNameStyle.padding.right = 10;
					m_TeamNameStyle.padding.top = 10;
				}
				return m_TeamNameStyle;
			}
		}
		protected GUIStyle m_TeamNameStyle = null;

		public GUIStyle TeamScoreStyle
		{
			get
			{
				if (m_TeamScoreStyle == null)
				{
					m_TeamScoreStyle = new GUIStyle("Label");
					m_TeamScoreStyle.font = Font;
					m_TeamScoreStyle.alignment = TextAnchor.MiddleLeft;
					m_TeamScoreStyle.fontSize = TeamScoreFontSize;
					m_TeamScoreStyle.wordWrap = false;
					m_TeamScoreStyle.padding.left = 10;
					m_TeamScoreStyle.padding.right = 10;
				}
				return m_TeamScoreStyle;
			}
		}
		protected GUIStyle m_TeamScoreStyle = null;


		#endregion

	}
}