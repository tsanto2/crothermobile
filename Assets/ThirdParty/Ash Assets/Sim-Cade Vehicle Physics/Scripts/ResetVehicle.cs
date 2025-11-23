using System.Collections;
using System.Collections.Generic;
//using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ashsvp
{
    public class ResetVehicle : MonoBehaviour
    {
        //[Scene] public string exitToThisScene;

        public void resetVehicle()
        {
            var pos = transform.position;
            pos.y += 1;
            transform.position = pos;
            transform.rotation = Quaternion.identity;
        }
        public void Quit()
        {
            //SceneManager.LoadScene(exitToThisScene);
        }
        public void ResetScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                resetVehicle();
            }
        }
    }
}
