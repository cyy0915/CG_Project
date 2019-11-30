//========= Copyright 2019, HTC Corporation. All rights reserved. ===========

using UnityEngine;


namespace HTC.UnityPlugin.FoveatedRendering
{
    public class ViveFoveatedGazeUpdater : MonoBehaviour
    {
        public static bool AttachGazeUpdater(GameObject obj)
        {
            if (obj != null)
            {
                var gazeUpdater = obj.GetComponent<ViveFoveatedGazeUpdater>();
                if(gazeUpdater == null)
                {
                    gazeUpdater = obj.AddComponent<ViveFoveatedGazeUpdater>();
                }

                gazeUpdater.enabled = true;

                return true;
            }
            return false;
        }
        
        void OnEnable()
        {

        }
        
        void Update()
        {
            //这里输入每帧的左右眼预测位置，位置为normolized的向量，如(0.0f, 0.0f, 1.0f)就是看向z轴正方向，坐标系为观察坐标系
            ViveFoveatedRenderingAPI.SetNormalizedGazeDirection(new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f));
            GL.IssuePluginEvent(ViveFoveatedRenderingAPI.GetRenderEventFunc(), (int)EventID.UPDATE_GAZE);
        }

    }
}