using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Diagnostics.Eventing.Reader;

// chair dependencies
using System.IO.Ports;
using KartGame.ChairSystems;

namespace KartGame.KartSystems
{
    /// <summary>
    /// A basic keyboard implementation of the IInput interface for all the input information a kart needs.
    /// </summary>
    public class KeyboardInput : MonoBehaviour, IInput
    {
        

        public float Acceleration
        {
            get { return m_Acceleration; }
        }
        public float Steering
        {
            get { return m_Steering; }
        }
        public bool BoostPressed
        {
            get { return m_BoostPressed; }
        }
        public bool FirePressed
        {
            get { return m_FirePressed; }
        }
        public bool HopPressed
        {
            get { return m_HopPressed; }
        }
        public bool HopHeld
        {
            get { return m_HopHeld; }
        }

        float m_Acceleration;
        float m_Steering;
        bool m_HopPressed;
        bool m_HopHeld;
        bool m_BoostPressed;
        bool m_FirePressed;
        public float pitch = 0;
        public float roll = 0;

        public float step = 0.8f;
        public float step_turn = 0.5f;

        bool m_FixedUpdateHappened;
        public Rigidbody car;
        SerialPort port;
        FutuRiftSerialPort my; 
        FromSource fromSource;
        public double previousHZ;
        private int state = 0;
        private float st_1, st_2, st_3;

        // chair coordinates changing method
        float getChairCoordinates(char direction)
        {
            switch (direction)
            {
                case 'U':
                    if (pitch <= 10)
                        return step;
                    break;
                case 'D':
                    if (pitch >= -10)
                        return -step;
                    break;
                case 'B':
                    while (pitch != 0)
                    {
                        if (pitch == step)
                            return -step;
                        else if (pitch == -step)
                            return step;
                        else if (pitch > 0)
                            return -2*step;
                        else
                            return 2*step;
                        
                    }
                    break;
                case 'L':
                    if (roll <= 8)
                        return step_turn;
                    break;
                case 'R':
                    if (roll >= -8)
                        return -step_turn;
                    break;
                    break;
                case 'I':
                    while (roll != 0)
                    {
                        if ((st_1 < 1) && (st_1 > -1))
                        {
                            roll = 0;
                            return 0;
                        }

                        if (roll == step_turn)
                            return -step_turn;
                        else if (roll == -step_turn)
                            return step;
                        else if (roll > 0)
                            return -2 * step_turn;
                        else
                            return 2 * step_turn;

                    }
                    break;
            }
            return 0;
        }

        void Start()
        {
            port = new SerialPort()
            {
                BaudRate = 115200,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                ReadBufferSize = 4096,
                WriteBufferSize = 4096,
                ReadTimeout = 500,
                PortName = "COM3",
            };
            port.Open();
            FutuRiftSerialPort my = new FutuRiftSerialPort(port);
            FromSource fromSource = new FromSource(port);
            my = new FutuRiftSerialPort(port);
            fromSource = new FromSource(port);
            float angle = 0.0f;
            var timer = new System.Timers.Timer(21);
            timer.Elapsed += (E, A) =>
            {
                //$"{angle} {pitch} {roll} {Length(pitch, roll)}"); 
                my.Control(pitch, roll);
            };
            timer.Start();

            car = GetComponent<Rigidbody>();
            previousHZ = Math.Sqrt(Math.Pow(car.velocity.x, 2) + Math.Pow(car.velocity.z, 2));
        }

        void Update ()
        {
            

            //Debug.Log(Camera.main.transform.X);
            //Debug.Log("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");


            if (Input.GetKey(KeyCode.UpArrow))
            {
                m_Acceleration = 1f;
                //pitch += getChairCoordinates('U');
                /*Debug.Log("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!")
               */
                state = 0;

            }
            else if (Input.GetKey (KeyCode.DownArrow))
            {
                m_Acceleration = -1f;
                //pitch += getChairCoordinates('D');
                state = 1;
            }  
            else
            {
                m_Acceleration = 0f;
                //pitch += getChairCoordinates('B');
            }

            if (Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow))
            {
                m_Steering = -1f;
                roll += getChairCoordinates('L');
            }
            else if (!Input.GetKey(KeyCode.LeftArrow) && Input.GetKey(KeyCode.RightArrow))
            {
                m_Steering = 1f;
                roll += getChairCoordinates('R');
            }
            else
            {
                m_Steering = 0f;
                roll += getChairCoordinates('I');
            }
                

            m_HopHeld = Input.GetKey (KeyCode.Space);

            if (m_FixedUpdateHappened)
            {
                m_FixedUpdateHappened = false;

                m_HopPressed = false;
                m_BoostPressed = false;
                m_FirePressed = false;
            }

            m_HopPressed |= Input.GetKeyDown (KeyCode.Space);
            m_BoostPressed |= Input.GetKeyDown (KeyCode.RightShift);
            m_FirePressed |= Input.GetKeyDown (KeyCode.RightControl);
        }

        void FixedUpdate ()
        {
            m_FixedUpdateHappened = true;
            //Debug.Log("HZ!!!!:    " + (previousHZ - Camera.main.gameObject.transform.position.magnitude)/Time.deltaTime);
            /*Debug.Log("XV:    " + car.velocity);
            Debug.Log("YA:    " + (previousHZ - Math.Abs(Camera.main.gameObject.transform.position.magnitude))/Time.deltaTime);
            Debug.Log("ZA:    " + Camera.main.gameObject.transform.eulerAngles.z);*/

            /*if (state == 0)
            {
                st_1 = (float)-(previousHZ - Math.Sqrt(Math.Pow(car.velocity.x, 2) + Math.Pow(car.velocity.z, 2))) / Time.deltaTime * 2;
                state = 1;
            }
            else if (state == 1)
            {
                st_2 = (float)-(previousHZ - Math.Sqrt(Math.Pow(car.velocity.x, 2) + Math.Pow(car.velocity.z, 2))) / Time.deltaTime * 2;
                state = 2;
            }
            else if (state == 2)
            {
                pitch = (st_1 + st_2) / 2;
                st_1 = (float)-(previousHZ - Math.Sqrt(Math.Pow(car.velocity.x, 2) + Math.Pow(car.velocity.z, 2))) / Time.deltaTime * 2;
                state = 1;

            }*/

            Debug.Log("WWW:    " + (float)-(previousHZ - Math.Sqrt(Math.Pow(car.velocity.x, 2) + Math.Pow(car.velocity.z, 2))) / Time.deltaTime * 2);
            st_3 = (float)-(previousHZ - Math.Sqrt(Math.Pow(car.velocity.x, 2) + Math.Pow(car.velocity.z, 2))) / Time.deltaTime * 2;
            if (Math.Abs(st_3 - st_2) > 0.5)
            {
                st_1 = st_3;
            }
            st_2 = (float)-(previousHZ - Math.Sqrt(Math.Pow(car.velocity.x, 2) + Math.Pow(car.velocity.z, 2))) / Time.deltaTime * 2;
            Debug.Log("STSTST:    " + st_1);
            Debug.Log("PPP:    "+ pitch);
            Debug.Log("PPP:    " + roll);
            previousHZ = Math.Sqrt(Math.Pow(car.velocity.x, 2) + Math.Pow(car.velocity.z, 2));

            if(state == 1)
            {
                if ((pitch > st_1 * 2) && (pitch > -15))
                {
                    pitch -= step;
                }
            }
            else
            {
                if ((pitch < st_1 * 2) && (pitch < 15))
                {

                    pitch += step;

                    //Debug.Log("+++++++++");
                }
            }

            if ((pitch > st_1*2) && (pitch > -15))
            {
                pitch -= step*2;
                //Debug.Log("--------");
                /*if (pitch == step)
                    pitch -= step;
                else if (pitch == -step)
                    pitch += step;
                else if (pitch > 0)
                    pitch -= 2 * step;
                else
                    pitch += 2 * step;*/
            }
            if ((st_1 < 1) && (st_1 > -1))
            {
                pitch = 0;
            }

        }


        void OnApplicationQuit()
        {
            port.Close();
        }
    }


    
}