using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class helloworld : MonoBehaviour
{
    public Transform in_transform;
    private float update;
    private long numOfFrames = 0;
    private Vector3 vel;
    private Vector3 acc;

    private const int MAX_NUM_VEL_DATA = 2000;
    private const int MAX_NUM_ACC_DATA = 4;

    //coefficients
    private const float k_x = -0.0015f;
    private const float k_y = -0.0053f;
    private const float l_x = 0.2491f;
    private const float l_y = 0.5293f;
    private const float deltaT_x = 0.1480f;
    private const float deltaT_y = 0.0304f;
    private const float beta_x = 0.0006f;
    private const float b_x = 0.0344f;
    private const float b_y = 0.0503f;
    private const float c_x = 0.1777f;
    private const float c_y = -2.5249f;

    private float [] velocity_x = new float [MAX_NUM_VEL_DATA];
    private float [] velocity_y = new float [MAX_NUM_VEL_DATA];
    private float [] acceleration_x = new float [MAX_NUM_ACC_DATA];
    private float [] acceleration_y = new float [MAX_NUM_ACC_DATA];
    private int index_vel_x = 0;
    private int index_vel_y = 0;
    private int index_acc_x = 0;
    private int index_acc_y = 0;
    private float std_deviation_vel_x = 0;
    private float std_deviation_vel_y = 0;
    private float expectation_square_vel_x = 0;
    private float expectation_square_vel_y = 0;
    private float expectation_vel_x = 0;
    private float expectation_vel_y = 0;

    private float mean_acc_x = 0;
    private float mean_acc_y = 0;

    private float alpha_x = 0;
    private float alpha_y = 0;
    private float velocity_predict_x = 0;
    private float velocity_predict_y = 0;

    private const int MAX_NUM_GAZE_POS = 120;
    private Vector2 [] gaze_pos_history = new Vector2 [MAX_NUM_GAZE_POS];
    private int index_gaze = 0;
    private Vector2 gaze_pos;
    private Vector2 mean_gaze_pos;
    // Start is called before the first frame update

    void Start()
    {
        InvokeRepeating("data_collect", 1.0f, 0.5f);
    }

    void test(){
        /*
        if(!OVRManager.isHmdPresent)
            return;
        if(!OVRManager.tracker.isPresent)
            return;
        if(!OVRManager.tracker.isPositionTracked)
            return;
        int numOfTracker = OVRManager.tracker.count;
        OVRPose tmpOVRPose;
        Vector3 tmpPos;
        Quaternion tmpQuat;
        for(int i = 0; i < numOfTracker; ++i){
            if(!(OVRManager.tracker.GetPresent(i) && OVRManager.tracker.GetPoseValid(i)))
                continue;
            tmpOVRPose = OVRManager.tracker.GetPose(i);
            Debug.Log("sensor position: " + tmpOVRPose.position);
            Debug.Log("sensor orientation: " + tmpOVRPose.orientation.eulerAngles);
        }*/
        vel = OVRManager.display.angularVelocity;
        acc = OVRManager.display.angularAcceleration;
        Debug.Log("Velocity:" + vel);
        Debug.Log("Acceleration:" + acc);
    }

    void data_collect(){
        vel = OVRManager.display.angularVelocity;
        acc = OVRManager.display.angularAcceleration;
        velocity_x[index_vel_x] = vel.y;
        velocity_y[index_vel_y] = -vel.x;
        acceleration_x[index_acc_x] = acc.y;
        acceleration_y[index_acc_y] = -acc.x;
        index_vel_x = (index_vel_x + 1) % MAX_NUM_VEL_DATA;
        index_vel_y = (index_vel_y + 1) % MAX_NUM_VEL_DATA;
        index_acc_x = (index_acc_x + 1) % MAX_NUM_ACC_DATA;
        index_acc_y = (index_acc_y + 1) % MAX_NUM_ACC_DATA;        
    }

    void predict_gaze_position(){
        
        float vel_current_x = 0;
        float vel_current_y = 0;
        /*
        vel_current_x = velocity_x[(index_vel_x + MAX_NUM_VEL_DATA - 1) % MAX_NUM_VEL_DATA];
        vel_current_y = velocity_y[(index_vel_y + MAX_NUM_VEL_DATA - 1) % MAX_NUM_VEL_DATA];
        */
        vel_current_x = vel.y;
        vel_current_y = -vel.x;

        if((vel_current_x < 0.5f && vel_current_x > -0.5f && vel_current_y <0.2f && vel_current_y > -0.2f)){
            gaze_pos.x = -0.05f;
            gaze_pos.y = -1.83f;
        }
        else if( (vel_current_x > 0.5f && vel_current_x < 83.8f || vel_current_x < -0.5f && vel_current_x > -88.5f)
              && (vel_current_y > 0.2f && vel_current_y < 36.0f || vel_current_y < -0.2f && vel_current_y > -35.6f) ){
            cal_mean_acc();
            velocity_predict_x = vel_current_x + mean_acc_x * deltaT_x;
            velocity_predict_y = vel_current_y + mean_acc_y * deltaT_y;
            //calculate alpha
            cal_std_deviation();
            alpha_x = k_x * std_deviation_vel_x + l_x;
            alpha_y = k_y * std_deviation_vel_y + l_y;
            gaze_pos.x = alpha_x * velocity_predict_x + beta_x * acc.x + c_x; //wait for Saliency map to be added
            gaze_pos.y = alpha_y * velocity_predict_y + c_y; //wait for Saliency map to be added
        }
        else{
            cal_mean_gaze_pos();
            gaze_pos = mean_gaze_pos;
        }
        gaze_pos_history[index_gaze] = gaze_pos;
        index_gaze = (index_gaze + 1) % MAX_NUM_GAZE_POS; 
    }

    void cal_std_deviation(){
        expectation_square_vel_x = 0;
        expectation_square_vel_y = 0;
        expectation_vel_x = 0;
        expectation_vel_y = 0;
        for(int i = 0; i < MAX_NUM_VEL_DATA; ++i)
            expectation_square_vel_x += Mathf.Pow(velocity_x[i], 2);
        for(int i = 0; i < MAX_NUM_VEL_DATA; ++i)
            expectation_square_vel_y += Mathf.Pow(velocity_y[i], 2);
        for(int i = 0; i < MAX_NUM_VEL_DATA; ++i)
            expectation_vel_x += velocity_x[i];
        for(int i = 0; i < MAX_NUM_VEL_DATA; ++i)
            expectation_vel_y += velocity_y[i];
        std_deviation_vel_x = Mathf.Sqrt(expectation_square_vel_x/MAX_NUM_VEL_DATA - Mathf.Pow((expectation_vel_x/MAX_NUM_VEL_DATA), 2));
        std_deviation_vel_y = Mathf.Sqrt(expectation_square_vel_y/MAX_NUM_VEL_DATA - Mathf.Pow((expectation_vel_y/MAX_NUM_VEL_DATA), 2));
    }

    void cal_mean_acc(){
        mean_acc_x = 0;
        mean_acc_y = 0;
        for(int i = 0; i < MAX_NUM_ACC_DATA; ++i)
            mean_acc_x += acceleration_x[i];
        for(int i = 0; i < MAX_NUM_ACC_DATA; ++i)
            mean_acc_y += acceleration_y[i];
        mean_acc_x /= MAX_NUM_ACC_DATA;
        mean_acc_y /= MAX_NUM_ACC_DATA;
    }

    void cal_mean_gaze_pos(){
        mean_gaze_pos.x = mean_gaze_pos.y = 0;
        for(int i = 0; i < MAX_NUM_GAZE_POS; ++i){
            mean_gaze_pos = mean_gaze_pos + gaze_pos_history[i];
        }
        mean_gaze_pos = mean_gaze_pos / MAX_NUM_GAZE_POS;
    }
    // Update is called once per frame
    void Update()
    {
        predict_gaze_position();
        Debug.Log("(" + gaze_pos.x + "," + gaze_pos.y + ")");
    }
}
