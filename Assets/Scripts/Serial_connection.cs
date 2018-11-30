using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class Serial_connection : MonoBehaviour
{

    private float lsm9ds1_ACCEL_2G = 0.000061f;
    private float lsm9ds1_MAG_4G = 0.00014f;
    private float lsm9ds1_GYRO_245DPS = (0.00875f);

    private readonly Vector3 GyroErr = new Vector3(1.8f, 3.2f, -0.5f);

    private float Gravity_Standard = 9.81f;
    private Vector3 Maxerror = Vector3.zero;
    private int count = 0;

    public Vector3 orientation;

    //private float totalRatio;
    //private int count;


    private SerialPort serial;
    [Tooltip("Millisecond timeout for reading from arduino")]
    public int msTimeout;

    private string serialPort;
    [Tooltip("Use this to specify inputs to be used")]
    public string Input;

    // Inspired by https://social.msdn.microsoft.com/Forums/vstudio/en-US/2a9f98c4-0bf0-4cb4-a257-11520522e5bd/how-to-open-port-with-c?forum=csharpgeneral

    // and https://www.alanzucconi.com/2015/10/07/how-to-integrate-arduino-with-unity/
    private void Awake()
    {
        serial = new SerialPort();

        List<string> ports = new List<string>(SerialPort.GetPortNames());
        foreach (string port in ports)
        {
            if (ConnectSerial(port))
            {
                Debug.Log("Found port " + serialPort);
                break;
            }
            else continue;
        }
        Debug.Log("Final Serial port value is " + serialPort);

    }

    private void ResponseHandler(object sender, SerialDataReceivedEventArgs args)
    {
        string x = serial.ReadExisting();
        Debug.Log("Read in " + x);
    }

    private string Read()
    {
        try
        {
            string readVal = serial.ReadLine();

            //Debug.Log("Performed proper read");
            serial.DiscardInBuffer();
            return readVal;
        }
        catch (System.TimeoutException)
        {

            Debug.Log("Timed out when reading");
            serial.DiscardInBuffer();
            return null;
        }
    }

    private bool ConnectSerial(string port)
    {
        try
        {
            Debug.Log("Attempting to open" + port);
            serialPort = port;
            serial = new SerialPort(port, 38400);
            serial.DataBits = 8;
            serial.Parity = Parity.None;
            serial.StopBits = StopBits.One;
            serial.Handshake = Handshake.XOnXOff;
            serial.ReadTimeout = msTimeout;
            serial.PortName = serialPort;
            serial.Open();
            serial.DiscardOutBuffer();
            serial.DiscardInBuffer();
            //serial.DataReceived += new SerialDataReceivedEventHandler(ResponseHandler); // not using the event method as it's finnicky in c#
        }
        catch (System.UnauthorizedAccessException exc)
        {
            Debug.Log("Had an error opening port " + serialPort);
            Debug.Log("Error was" + exc.Message);
            return false;
        }
        catch (System.ArgumentOutOfRangeException exc)
        {
            Debug.Log("Had an error opening port " + serialPort);
            Debug.Log("Error was" + exc.Message);
            return false;
        }
        catch (System.ArgumentException exc)
        {
            Debug.Log("Had an error opening port " + serialPort);
            Debug.Log("Error was" + exc.ToString());
            Debug.Log("Error came from source " + exc.StackTrace);
            return false;
        }
        catch (System.IO.IOException exc)
        {
            Debug.Log("Had an error opening port " + serialPort);
            Debug.Log("Error was" + exc.Message);
            return false;
        }
        catch (System.InvalidOperationException exc)
        {
            Debug.Log("Had an error opening port " + serialPort);
            Debug.Log("Error was" + exc.Message);
            return false;
        }
        catch (System.Exception exc)
        {
            Debug.Log("Had an error opening port " + serialPort);
            Debug.Log("Error was" + exc.Message);
            return false;
        }
        return true;

    }


    // Update is called once per frame
    private void Update()
    {
        if (!serial.IsOpen)
        {
            ConnectSerial(serialPort);
        }
        else
        {

            serial.WriteLine(Input);
            serial.BaseStream.Flush();
            serial.DiscardOutBuffer();

            Thread.Sleep(2); // 

            string inputs = Read();
            if (inputs == null || inputs.Length == 0)
            {
                Debug.Log("Input received was null");
            }
            else
            {
                //Debug.Log("Trying to split " + inputs);
                string[] A_M = inputs.Split(';');
                string[] A_vals = A_M[0].Split(',');
                string[] M_vals = A_M[1].Split(',');
                string[] E_vals = A_M[2].Split(',');

                Vector3 A_val_processed = new Vector3();
                Vector3 M_val_processed = new Vector3();
                Vector3 E_val_processed = new Vector3();
                for (int i = 0; i < A_vals.Length; i++)
                {
                    A_val_processed[i] = float.Parse(A_vals[i]) * lsm9ds1_ACCEL_2G;// * Gravity_Standard;
                    M_val_processed[i] = float.Parse(M_vals[i]) * lsm9ds1_MAG_4G;
                    E_val_processed[i] = (float.Parse(E_vals[i])) * lsm9ds1_GYRO_245DPS - GyroErr[i];
                    E_val_processed[i] = E_val_processed[i] < 1f ? 0 : E_val_processed[i];
                }

                for (int i = 0; i < A_vals.Length; i++)
                {

                    //Debug.Log("Accel : " + (A_val_processed[0] + "," + A_val_processed[1] + "," + A_val_processed[2]));                    
                    //Debug.Log("Magnet: " + (M_val_processed[0] + "," + M_val_processed[1] + "," + M_val_processed[2]));

                    //count++;
                    /*
                    if (count > 1000)
                    {
                        Maxerror = Maxerror.sqrMagnitude > (E_val_processed - GyroErr).sqrMagnitude ? Maxerror : (E_val_processed - GyroErr);
                        Debug.Log("Gyros : " + (E_val_processed[0] + "," + E_val_processed[1] + "," + E_val_processed[2]));
                        Debug.Log("Normalised value is " + Maxerror);
                    }
                    */
                    count++;
                    if (count > 1000)
                    {
                        Debug.Log("Gyro : " + E_val_processed);
                        Debug.Log("Gyro : " + float.Parse(E_vals[0]) * lsm9ds1_GYRO_245DPS + "," + float.Parse(E_vals[1]) * lsm9ds1_GYRO_245DPS + "," + float.Parse(E_vals[2]) * lsm9ds1_GYRO_245DPS);
                        gameObject.transform.rotation *= new Quaternion(Time.deltaTime * E_val_processed[0] * 0.5f, Time.deltaTime * E_val_processed[1] * 0.5f, Time.deltaTime * E_val_processed[2] * 0.5f, 1);
                    }

                    orientation = (new Vector3(A_val_processed[0], A_val_processed[1], A_val_processed[2])).normalized * 90;
                    //gameObject.transform.eulerAngles = orientation;

                }

                //Debug.Log("Input received is " + inputs + " and length of input is " + inputs.Length);
                //Debug.Log("Received non zero input");
            }
        }
    }
}
