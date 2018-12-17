using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class Serial_connection : MonoBehaviour
{
    #region inputs
    [Tooltip("Number of frames that we ignore readings for")]
    [Range(1, 100)]
    public int invulCount = 10;

    [Tooltip("Threshold required for accepting motion")]
    [Range(1.0f, 3.0f)]
    public float noiseThreshold = 1.5f;

    [Tooltip("Millisecond timeout for reading from arduino")]
    public int msTimeout;

    private string serialPort;
    [Tooltip("Use this to specify inputs to be used")]
    public string Input;
    #endregion

    #region Constants
    private readonly float lsm9ds1_ACCEL_2G = 0.000061f;
    private readonly float lsm9ds1_MAG_4G = 0.00014f;
    private readonly float lsm9ds1_GYRO_245DPS = (0.00875f);
    private readonly float Gravity_Standard = 9.81f;
    private readonly Vector3 Gravity_Base = new Vector3(0.1f, 10.2f, -0.5f);
    #endregion

    private Vector3 currPoint;
    private Vector3 endPoint;

    #region variables
    private int invulFrames = 0;
    private int count = 0;
    private Vector3 prevAcc = Vector3.zero;
    private Vector3 prevGyro = new Vector3(2.11535f, -0.465574f, 3.859f);
    private Vector3 prevLinAcc = Vector3.zero;
    private RedirectPad pad;
    private RedirectPad curr;
    private SerialPort serial;
    #endregion
    // Inspired by https://social.msdn.microsoft.com/Forums/vstudio/en-US/2a9f98c4-0bf0-4cb4-a257-11520522e5bd/how-to-open-port-with-c?forum=csharpgeneral

    // and https://www.alanzucconi.com/2015/10/07/how-to-integrate-arduino-with-unity/
    private void Awake()
    {
        serial = new SerialPort();

        currPoint = Vector3.zero;
        endPoint = Vector3.zero;

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

        #region various pads
        pad = new RedirectPad();
        pad.current = "Center";
        pad.thisObj = GameObject.Find("PlaneC");

        // North pad
        pad.North = new RedirectPad();
        pad.North.thisObj = GameObject.Find("PlaneN");
        pad.North.current = "North";
        pad.North.East = new RedirectPad();
        pad.North.East.current = "North-East";
        pad.North.West = new RedirectPad();
        pad.North.West.current = "North-West";
        pad.North.South = pad;
        pad.North.North = pad.North;

        // South pad
        pad.South = new RedirectPad();
        pad.South.thisObj = GameObject.Find("PlaneS");
        pad.South.current = "South";
        pad.South.East = new RedirectPad();
        pad.South.East.current = "South-East";
        pad.South.West = new RedirectPad();
        pad.South.West.current = "South-West";
        pad.South.South = pad.South;
        pad.South.North = pad;

        //East pad
        pad.East = new RedirectPad();
        pad.East.thisObj = GameObject.Find("PlaneE");
        pad.East.current = "East";
        pad.East.East = pad.East;
        pad.East.West = pad;
        pad.East.South = pad.South.East;
        pad.East.North = pad.North.East;

        //West pad
        pad.West = new RedirectPad();
        pad.West.thisObj = GameObject.Find("PlaneW");
        pad.West.current = "West";
        pad.West.East = pad;
        pad.West.West = pad.West;
        pad.West.South = pad.South.West;
        pad.West.North = pad.North.West;

        //North-East pad
        pad.North.East.thisObj = GameObject.Find("PlaneNE");
        pad.North.East.East = pad.North.East;
        pad.North.East.West = pad.North;
        pad.North.East.South = pad.East;
        pad.North.East.North = pad.North.East;

        //North-West pad
        pad.North.West.thisObj = GameObject.Find("PlaneNW");
        pad.North.West.East = pad.North;
        pad.North.West.West = pad.North.West;
        pad.North.West.South = pad.West;
        pad.North.West.North = pad.North.West;

        //South-East pad
        pad.South.East.thisObj = GameObject.Find("PlaneSE");
        pad.South.East.East = pad.South.East;
        pad.South.East.West = pad.South;
        pad.South.East.South = pad.South.East;
        pad.South.East.North = pad.East;

        //South-West pad
        pad.South.West.thisObj = GameObject.Find("PlaneSW");
        pad.South.West.East = pad.South;
        pad.South.West.West = pad.South.West;
        pad.South.West.North = pad.West;
        pad.South.West.South = pad.South.West;
        #endregion
        curr = pad;
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
            if (invulFrames == 0)
            {
                #region write
                serial.WriteLine(Input);
                serial.BaseStream.Flush();
                serial.DiscardOutBuffer();

                Thread.Sleep(4); // 
                #endregion

                #region read
                string inputs = Read();
                if (inputs == null || inputs.Length == 0)
                {
                    //Debug.Log("Input received was null");
                }
                else //if (inputs.Length > 30)
                {
                    #region processing
                    string[] A_M = inputs.Split(';');
                    string[] A_vals = A_M[0].Split(',');
                    string[] G_vals = A_M[2].Split(',');

                    Vector3 A_val_processed = new Vector3();
                    Vector3 G_val_processed = new Vector3();
                    {

                        A_val_processed[0] = float.Parse(A_vals[0]) * lsm9ds1_ACCEL_2G * Gravity_Standard;
                        A_val_processed[2] = float.Parse(A_vals[1]) * lsm9ds1_ACCEL_2G * Gravity_Standard;
                        A_val_processed[1] = float.Parse(A_vals[2]) * lsm9ds1_ACCEL_2G * Gravity_Standard;

                        G_val_processed[0] = float.Parse(G_vals[0]) * lsm9ds1_GYRO_245DPS;
                        G_val_processed[2] = float.Parse(G_vals[1]) * lsm9ds1_GYRO_245DPS;
                        G_val_processed[1] = float.Parse(G_vals[2]) * lsm9ds1_GYRO_245DPS;

                    }
                    #endregion
                    if (count < 300)
                    {
                        count++;
                        prevAcc += (G_val_processed - prevGyro);
                        Debug.Log("Counting");

                    }
                    else
                    {
                        #region calculate Gyro readings
                        Vector3 rotate = new Vector3(
                            0.98f * (G_val_processed[0] - prevGyro.x - prevAcc.x / count) * Time.deltaTime + 0.02f * Mathf.Atan2(A_val_processed.y, A_val_processed.x),
                            0.98f * (G_val_processed[1] - prevGyro.y - prevAcc.y / count) * Time.deltaTime + 0.02f * Mathf.Atan2(A_val_processed.z, A_val_processed.y),
                            0.98f * (G_val_processed[2] - prevGyro.z - prevAcc.z / count) * Time.deltaTime + 0.02f * Mathf.Atan2(A_val_processed.x, A_val_processed.z)
                        );

                        if (rotate.magnitude > 0.13f)
                        {
                            //Debug.Log("Value read from gyro is " + (G_val_processed - prevGyro));
                            gameObject.transform.Rotate(rotate);
                        }
                        else
                        {
                            count++;
                            prevAcc += (G_val_processed - prevGyro);
                        }
                        Vector3 acc_fin = A_val_processed - gameObject.transform.rotation * Gravity_Base;
                        #endregion
                        //Debug.Log("Linear acceleration is " + acc_fin.x + "," + acc_fin.y + "," + acc_fin.z);

                        Vector3 delta = acc_fin - prevLinAcc;


                        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y) && Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                        {
                            //Debug.Log("Greatest along x");
                            if (delta.x > 1.5f)
                            {
                                Debug.Log("Going to " + curr.North.current + " from " + curr.current);
                                gameObject.transform.position = curr.North.thisObj.transform.position;
                                curr = curr.North;
                                invulFrames = invulCount;
                            }
                            else if (delta.x < -1.5f)
                            {
                                Debug.Log("Going to " + curr.South.current + " from " + curr.current);
                                gameObject.transform.position = curr.South.thisObj.transform.position;
                                curr = curr.South;
                                invulFrames = invulCount;
                            }
                        }
                        else if (Mathf.Abs(delta.y) > Mathf.Abs(delta.z))
                        {
                            //Debug.Log("Greatest along y");
                            /*if (delta.y > 2f)
                            {
                                gameObject.transform.position = pad.East.thisObj.transform.position;
                            }
                            else if (delta.y < -2f)
                            {
                                gameObject.transform.position = pad.West.thisObj.transform.position;
                            }*/

                        }
                        else
                        {
                            // Debug.Log("Greatest along z");
                            if (delta.z > 1.5f)
                            {
                                Debug.Log("Going to " + curr.East.current + " from " + curr.current);
                                gameObject.transform.position = curr.East.thisObj.transform.position;
                                curr = curr.East;
                                invulFrames = invulCount;
                            }
                            else if (delta.z < -1.5f)
                            {
                                Debug.Log("Going to " + curr.West.current + " from " + curr.current);
                                gameObject.transform.position = curr.West.thisObj.transform.position;
                                curr = curr.West;
                                invulFrames = invulCount;
                            }

                        }
                        prevLinAcc = acc_fin;
                    }
                }
            }
            else
            {
                invulFrames--;
            }
        }
        #endregion
    }

    private IEnumerator moveLerp()
    {
        float val = 0f;

        while (true)
        {

            Vector3 pos = gameObject.transform.position;

            if (endPoint != currPoint)
            {
                if (endPoint.x < currPoint.x)
                { // endpoint.x < currpoint.x
                    pos.x = Mathf.Lerp(endPoint.x, currPoint.x, val);
                }
                else
                { // endpoint.x > currpoint.x
                    pos.x = Mathf.Lerp(currPoint.x, endPoint.x, val);
                }

                if (endPoint.y < currPoint.y)
                { // endpoint.x < currpoint.x
                    pos.y = Mathf.Lerp(endPoint.y, currPoint.y, val);
                }
                else
                { // endpoint.x > currpoint.x
                    pos.y = Mathf.Lerp(currPoint.y, endPoint.y, val);
                }


                if (endPoint.z < currPoint.z)
                { // endpoint.z < currpoint.z
                    pos.z = Mathf.Lerp(endPoint.z, currPoint.z, val);
                }
                else
                { // endpoint.z > currpoint.z
                    pos.z = Mathf.Lerp(currPoint.z, endPoint.z, val);
                }
                val += 0.05f;
            }
            else
            {
                val = 0f;
            }
            yield return null;
        }


    }
}

public class RedirectPad
{
    public string current;

    public GameObject thisObj;

    public RedirectPad North;
    public RedirectPad East;
    public RedirectPad West;
    public RedirectPad South;

}