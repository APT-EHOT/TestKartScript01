using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        public float step = 0.1f;

        bool m_FixedUpdateHappened;

        SerialPort port;
        FutuRiftSerialPort my; 
        FromSource fromSource;

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
                    if (pitch >= 10)
                        return -step;
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
                //System.Console.WriteLine($"{angle} {pitch} {roll} {Length(pitch, roll)}");
                my.Control(pitch, roll);
            };
            timer.Start();
        }

        void Update ()
        {

            if (Input.GetKey(KeyCode.UpArrow))
            {
                m_Acceleration = 1f;
                pitch += getChairCoordinates('U');
            }
            else if (Input.GetKey (KeyCode.DownArrow))
            {
                m_Acceleration = -1f;
                pitch += getChairCoordinates('D');
            }  
            else
            {
                m_Acceleration = 0f;
                pitch = -18;
            }

            if (Input.GetKey (KeyCode.LeftArrow) && !Input.GetKey (KeyCode.RightArrow))
                m_Steering = -1f;
            else if (!Input.GetKey (KeyCode.LeftArrow) && Input.GetKey (KeyCode.RightArrow))
                m_Steering = 1f;
            else
                m_Steering = 0f;

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
        }
    }
}