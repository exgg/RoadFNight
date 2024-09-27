using Gley.PedestrianSystem;
using Photon.Realtime;
using Redcode.Pools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoadfnightPedestrian
{
    /*
     * Highest level brain 
     */

    public class PedestrianSystem : MonoBehaviour
    {
        public CityGen myCity;
        public List<GameObject> player_lst = new List<GameObject>();
        public float visible_threshold = 10;

        public List<PedestrianGroup> group_lst = new List<PedestrianGroup>();

        private void Start()
        {
            init_block_pedestrian();//init secondary brain
        }

        private void FixedUpdate()
        {
            foreach (var group in group_lst)
            {
                group.update_members();
            }
        }

        private void init_block_pedestrian()
        {
            foreach (var block in myCity.listOfBlocks)
            {
                foreach (var pedestrian in GetComponentsInChildren<Pedestrian>(true))
                {
                    block.try_add_pedestrian(pedestrian);
                }
            }

        }
    }
}

