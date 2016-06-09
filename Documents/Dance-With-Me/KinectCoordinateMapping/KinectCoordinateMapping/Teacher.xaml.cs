using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Media;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Microsoft.Win32;
//using AForge.Video.FFMPEG;

using VVVV.Utils;

//using Bespoke.Common.Osc;
//using Transmitter;

//using Toub.Sound.Midi;
//using Midi;

namespace KinectCoordinateMapping
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Teacher : Window
    {
        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        IList<Body> _bodies;

        CameraMode _mode = CameraMode.Color;
        Boolean showSkeleton = true;
        Boolean showVideo = true;
        Boolean hearAudio = true;
        Boolean writeToFile = false;
        Boolean mediaPlayerIsPlaying = false;
        Char mode = 'T';

        private static List<List<double>> student_limb_angles = new List<List<double>>();

       // private bool mediaPlayerIsPlaying = false;
        private bool userIsDraggingSlider = false;

        public static readonly int Port = 6448; //port of Wekinator
        private static readonly string TestMethod = "/oscCustomFeatures";

        public Teacher()
        {
            InitializeComponent();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

            }

            video.MediaEnded+=video_MediaEnded;
            btn_TeacherRecord.Click+=btn_TeacherRecord_Click;

            btn_Dance1.Visibility = System.Windows.Visibility.Hidden;
            btn_Dance2.Visibility = System.Windows.Visibility.Hidden;
            btn_Dance3.Visibility = System.Windows.Visibility.Hidden;
            btn_Dance4.Visibility = System.Windows.Visibility.Hidden;
            cbox_Video.Visibility = System.Windows.Visibility.Hidden;
            cbox_Audio.Visibility = System.Windows.Visibility.Hidden;
            lbl_Score.Visibility = System.Windows.Visibility.Hidden;
            cbox_Audio.IsChecked = true;
            cbox_Video.IsChecked = true;
            videoGrid.Visibility = System.Windows.Visibility.Hidden;
            btn_TeacherRecord.Visibility = System.Windows.Visibility.Visible;
            cmbox_dances.Visibility = System.Windows.Visibility.Visible;
        }

        private void btn_TeacherRecord_Click(object sender, RoutedEventArgs e)
        {
            if (btn_TeacherRecord.Content.ToString() == "Record Limb Angles")
            {
                ComboBoxItem ComboItem = (ComboBoxItem)cmbox_dances.SelectedItem;
                string dance_filename = ComboItem.Content.ToString().ToLower().Replace(" ", "");
                string path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\Teacher_Files\" + dance_filename + ".txt";

                // empty file if it currently exists
                if (File.Exists(path))
                    File.WriteAllText(path, string.Empty);

                btn_TeacherRecord.Content = "Stop";
            }
            else
                btn_TeacherRecord.Content = "Record Limb Angles";

            writeToFile = !(writeToFile);
        }

        private void video_MediaEnded(object sender, RoutedEventArgs e)
        {
            //video.Position = TimeSpan.FromSeconds(0); //restart video
            ComboBoxItem ComboItem = (ComboBoxItem)cmbox_dances.SelectedItem;
            string dance_filename = ComboItem.Content.ToString().ToLower().Replace(" ", "");

            int score = calculateAccuracy(dance_filename);
            lbl_Score.Content = "Score: " + score;
        }

        private void player_Click(object sender, RoutedEventArgs e)
        {
            if (sender == btn_Pause)
            {
                if (mediaPlayerIsPlaying == true)
                {
                    video.Pause();
                    mediaPlayerIsPlaying = false;
                }
                else
                {
                    video.Play();
                    mediaPlayerIsPlaying = true;
                }
            }

            if (sender == btn_Start)
            {
                video.Position = TimeSpan.FromSeconds(0); //restart video
                video.Play();
                student_limb_angles.Clear();
                lbl_Score.Content = "Score: -";
                mediaPlayerIsPlaying = true;
            }
            if (sender == btn_Stop)
            {
                video.Stop();

                ComboBoxItem ComboItem = (ComboBoxItem)cmbox_dances.SelectedItem;
                string dance_filename = ComboItem.Content.ToString().ToLower().Replace(" ", "");
                calculateAccuracy(dance_filename);
                mediaPlayerIsPlaying = false;
            }
            if(sender == btn_Mute)
            {
                if (video.Volume > 0) //not mute
                {
                    video.Volume = 0;
                    img_mute.Visibility = System.Windows.Visibility.Visible;
                }
                else
                    if (video.Volume == 0) //mute
                    {
                        video.Volume = 50;
                        img_mute.Visibility = System.Windows.Visibility.Hidden;
                    }
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if ((video.Source != null) && (video.NaturalDuration.HasTimeSpan) && (!userIsDraggingSlider))
            {
                sliProgress.Minimum = 0;
                sliProgress.Maximum = video.NaturalDuration.TimeSpan.TotalSeconds;
                sliProgress.Value = video.Position.TotalSeconds;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_reader != null)
            {
                _reader.Dispose();
            }

            if (_sensor != null)
            {
                _sensor.Close();
            }
        }

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            // Color
            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == CameraMode.Color)
                    {
                        camera.Source = frame.ToBitmap();

                        //if (writeToFile)
                        //{
                        //    VideoFileWriter writer = new VideoFileWriter();
                        //    // create new video file
                        //    writer.Open("dance1.avi", 320, 240, 30, VideoCodec.MPEG4);

                        //    // create a bitmap to save into the video file
                        //    System.Drawing.Bitmap image = ColorExtensions.BitmapFromSource(bmap);
                        //    writer.WriteVideoFrame(image);

                        //    writer.Close();
                        //}
                    }
                }
            }

            // Depth
            using (var frame = reference.DepthFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == CameraMode.Depth)
                    {
                        camera.Source = frame.ToBitmap();
                    }
                }
            }

            // Infrared
            using (var frame = reference.InfraredFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == CameraMode.Infrared)
                    {
                        camera.Source = frame.ToBitmap();
                    }
                }
            }

            // Body
            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    canvas.Children.Clear();

                    _bodies = new Body[frame.BodyFrameSource.BodyCount];

                    frame.GetAndRefreshBodyData(_bodies);

                    foreach (var body in _bodies)
                    {
                        if (body.IsTracked)
                        {
                            //Play_Notes(body);

                            if(showSkeleton==true)
                                DrawSkeleton(body);

                            try
                            {
                                OSC_Send_Limbs(body);
                            }
                            catch (BadImageFormatException ex)
                            {
                                Console.WriteLine("Unable to load {0}.");
                                Console.WriteLine(ex.Message.Substring(0,
                                                  ex.Message.IndexOf(".") + 1));
                            }
                           
                        }
                    }
                }
            }
        }

        private void Play_Notes(Body body)
        {
            // Find the joints
            Joint handRight = body.Joints[JointType.HandRight];
            Joint handLeft = body.Joints[JointType.HandLeft];

             //3D space point
            CameraSpacePoint handRightJointPosition = handRight.Position;
            CameraSpacePoint handLeftJointPosition = handLeft.Position;

                 //2D space point
            Point handRightPoint = new Point();
            Point handLeftPoint = new Point();

            if (_mode == CameraMode.Color)
            {
                ColorSpacePoint handRightColorPoint = _sensor.CoordinateMapper.MapCameraPointToColorSpace(handRightJointPosition);
                ColorSpacePoint handLeftColorPoint = _sensor.CoordinateMapper.MapCameraPointToColorSpace(handLeftJointPosition);

                handRightPoint.X = float.IsInfinity(handRightColorPoint.X) ? 0 : handRightColorPoint.X;
                handRightPoint.Y = float.IsInfinity(handRightColorPoint.Y) ? 0 : handRightColorPoint.Y;
                handLeftPoint.X = float.IsInfinity(handLeftColorPoint.X) ? 0 : handLeftColorPoint.X;
                handLeftPoint.Y = float.IsInfinity(handLeftColorPoint.Y) ? 0 : handLeftColorPoint.Y;
            }
            else if (_mode == CameraMode.Depth || _mode == CameraMode.Infrared) // Change the Image and Canvas dimensions to 512x424
            {
                DepthSpacePoint handRightDepthPoint = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(handRightJointPosition);
                DepthSpacePoint handLeftDepthPoint = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(handLeftJointPosition);

                handRightPoint.X = float.IsInfinity(handRightDepthPoint.X) ? 0 : handRightDepthPoint.X;
                handRightPoint.Y = float.IsInfinity(handRightDepthPoint.Y) ? 0 : handRightDepthPoint.Y;
                handLeftPoint.X = float.IsInfinity(handLeftDepthPoint.X) ? 0 : handLeftDepthPoint.X;
                handLeftPoint.Y = float.IsInfinity(handLeftDepthPoint.Y) ? 0 : handLeftDepthPoint.Y;
            }

            switch (body.HandRightState)
            {
                case HandState.Open:
                    //stop music from right hand
                    {
                        SystemSounds.Exclamation.Play();
                        Thread.Sleep(1000);
                    }
                    break;

                case HandState.Closed:
                    if (handRightPoint.X > System.Windows.SystemParameters.PrimaryScreenWidth / 2)
                    {
                        SystemSounds.Beep.Play();
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        SystemSounds.Asterisk.Play(); 
                        Thread.Sleep(1000);
                    }
                    
                    break;
                case HandState.Lasso:

                    break;
                default:
                    break;
            }

            switch (body.HandLeftState)
            {
                case HandState.Open:
                    SystemSounds.Exclamation.Play();
                    Thread.Sleep(1000);
                    break;
                case HandState.Closed:
                    if (handLeftPoint.X > System.Windows.SystemParameters.PrimaryScreenWidth / 2)
                    {
                        SystemSounds.Hand.Play();
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        SystemSounds.Question.Play();
                        Thread.Sleep(1000);
                    }
                    break;

                default:
                    break;
            }
        }

        public void DrawSkeleton(Body body)
        {
            if (body == null) return;

                foreach (Joint joint in body.Joints.Values)
                {
                    DrawPoint(joint);
                }

                DrawLine(body.Joints[JointType.Head], body.Joints[JointType.Neck]);
                DrawLine(body.Joints[JointType.Neck], body.Joints[JointType.SpineShoulder]);
                DrawLine(body.Joints[JointType.SpineShoulder], body.Joints[JointType.ShoulderLeft]);
                DrawLine(body.Joints[JointType.SpineShoulder], body.Joints[JointType.ShoulderRight]);
                DrawLine(body.Joints[JointType.SpineShoulder], body.Joints[JointType.SpineMid]);
                DrawLine(body.Joints[JointType.ShoulderLeft], body.Joints[JointType.ElbowLeft]);
                DrawLine(body.Joints[JointType.ShoulderRight], body.Joints[JointType.ElbowRight]);
                DrawLine(body.Joints[JointType.ElbowLeft], body.Joints[JointType.WristLeft]);
                DrawLine(body.Joints[JointType.ElbowRight], body.Joints[JointType.WristRight]);
                DrawLine(body.Joints[JointType.WristLeft], body.Joints[JointType.HandLeft]);
                DrawLine(body.Joints[JointType.WristRight], body.Joints[JointType.HandRight]);
                DrawLine(body.Joints[JointType.HandLeft], body.Joints[JointType.HandTipLeft]);
                DrawLine(body.Joints[JointType.HandRight], body.Joints[JointType.HandTipRight]);
                DrawLine(body.Joints[JointType.WristLeft], body.Joints[JointType.ThumbLeft]);
                DrawLine(body.Joints[JointType.WristRight], body.Joints[JointType.ThumbRight]);
                DrawLine(body.Joints[JointType.SpineMid], body.Joints[JointType.SpineBase]);
                DrawLine(body.Joints[JointType.SpineBase], body.Joints[JointType.HipLeft]);
                DrawLine(body.Joints[JointType.SpineBase], body.Joints[JointType.HipRight]);
                DrawLine(body.Joints[JointType.HipLeft], body.Joints[JointType.KneeLeft]);
                DrawLine(body.Joints[JointType.HipRight], body.Joints[JointType.KneeRight]);
                DrawLine(body.Joints[JointType.KneeLeft], body.Joints[JointType.AnkleLeft]);
                DrawLine(body.Joints[JointType.KneeRight], body.Joints[JointType.AnkleRight]);
                DrawLine(body.Joints[JointType.AnkleLeft], body.Joints[JointType.FootLeft]);
                DrawLine(body.Joints[JointType.AnkleRight], body.Joints[JointType.FootRight]);
        }

        public void DrawPoint(Joint joint)
        {

            if (joint.TrackingState == TrackingState.Tracked)
            {
                //scale point
                double factorX = 1;
                double factorY = 1;

                //joint = ScaleTo(joint, camera.ActualWidth, camera.ActualHeight);

                //joint.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);

                //3D space point
                CameraSpacePoint jointPosition = joint.Position;

                //2D space point
                Point point = new Point();

                if (_mode == CameraMode.Color)
                {
                    ColorSpacePoint colorPoint = _sensor.CoordinateMapper.MapCameraPointToColorSpace(jointPosition);

                    point.X = float.IsInfinity(colorPoint.X) ? 0 : colorPoint.X;
                    point.Y = float.IsInfinity(colorPoint.Y) ? 0 : colorPoint.Y;

                    //scale point
                    factorX = 1920.0 / camera.ActualWidth;
                    factorY = 1080.0 / camera.ActualHeight;

                }
                else if (_mode == CameraMode.Depth || _mode == CameraMode.Infrared) // Change the Image and Canvas dimensions to 512x424
                {
                    DepthSpacePoint depthPoint = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(jointPosition);

                    point.X = float.IsInfinity(depthPoint.X) ? 0 : depthPoint.X;
                    point.Y = float.IsInfinity(depthPoint.Y) ? 0 : depthPoint.Y;

                    //scale point
                    factorX = 512.0 / camera.ActualWidth;
                    factorY = 424.0 / camera.ActualHeight;
                }

                //if (joint.Position.X != 0 & joint.Position.Y != 0)
                    if (point.X != 0 & point.Y != 0)
                {
                    //Draw
                    Ellipse ellipse = new Ellipse
                    {
                        Fill = Brushes.LightBlue,
                        Width = 15,
                        Height = 15
                    };

                    Canvas.SetLeft(ellipse, (point.X/factorX) - ellipse.Width / 2);
                    Canvas.SetTop(ellipse, (point.Y/factorY) - ellipse.Height / 2);

                    //Canvas.SetLeft(ellipse, joint.Position.X - ellipse.Width / 2);
                    //Canvas.SetTop(ellipse, joint.Position.Y - ellipse.Height / 2);

                    canvas.Children.Add(ellipse);
                }
                //WriteJointToFile(joint);
            }
        }

        public void WriteLimbAnglesToFile(List<double> limb_angles, string filename)
        {
            //write to file
            string r = "";
            string path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\Teacher_Files\" + filename+".txt";
            
            foreach (double i in limb_angles)
                r += i.ToString() + "\t";

            // This text is always added, making the file longer over time 
            // if it is not deleted. 
            File.AppendAllText(path, r);
        }

        public List<List<double>> ReadLimbAngles(string filename)
        {
            string path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\Teacher_Files\" + filename+".txt";

            List<List<double>> all_limb_angles = new List<List<double>>();

            //Open the file to read from. 
            string readText = File.ReadAllText(path);
            List<string> listStrLineElements = readText.Split('\t').ToList();

            int j = 0;
            List<double> limb_angles = new List<double>();

            foreach (string i in listStrLineElements)
            {
                if (i != "")
                {
                    double angle = Convert.ToDouble(i);
                    limb_angles.Add(angle);
                    j += 1;

                    if (j == 15) //number of limb angles in one frame
                    {
                        all_limb_angles.Add(limb_angles);
                        j = 0;

                        limb_angles = new List<double>();
                    }
                }
            }

            return all_limb_angles;
        }

        public void DrawLine(Joint first, Joint second)
        {
            if (first.TrackingState == TrackingState.NotTracked || second.TrackingState == TrackingState.NotTracked) return;

            //first = ScaleTo(first, camera.ActualWidth, camera.ActualHeight);
            //second = ScaleTo(second, camera.ActualWidth, camera.ActualHeight);

            CameraSpacePoint cameraPointFirst = first.Position;
            CameraSpacePoint cameraPointSecond = second.Position;

            ColorSpacePoint colorPointFirst = _sensor.CoordinateMapper.MapCameraPointToColorSpace(cameraPointFirst);
            ColorSpacePoint colorPointSecond = _sensor.CoordinateMapper.MapCameraPointToColorSpace(cameraPointSecond);

            Point pointFirst = new Point();
            Point pointSecond = new Point();

            if (_mode == CameraMode.Color)
            {
                pointFirst.X = float.IsInfinity(colorPointFirst.X) ? 0 : colorPointFirst.X;
                pointFirst.Y = float.IsInfinity(colorPointFirst.Y) ? 0 : colorPointFirst.Y;

                pointSecond.X = float.IsInfinity(colorPointSecond.X) ? 0 : colorPointSecond.X;
                pointSecond.Y = float.IsInfinity(colorPointSecond.Y) ? 0 : colorPointSecond.Y;

            }

            if (pointFirst.X != 0 & pointFirst.Y != 0 & pointSecond.X != 0 & pointSecond.Y != 0)
            //if (first.Position.X != 0 & first.Position.Y != 0 & second.Position.X != 0 & second.Position.Y != 0)
            {

                //scale point
                double factorX = 1920.0 / camera.ActualWidth;
                double factorY = 1080.0 / camera.ActualHeight;

                Line line = new Line
                {
                    X1 = pointFirst.X/factorX,
                    Y1 = pointFirst.Y/factorY,
                    X2 = pointSecond.X/factorX,
                    Y2 = pointSecond.Y/factorY,
                    //X1 = first.Position.X,
                    //Y1 = first.Position.Y,
                    //X2 = second.Position.X,
                    //Y2 = second.Position.Y,
                    StrokeThickness = 3,
                    Stroke = new SolidColorBrush(Colors.LightBlue)
                };

                canvas.Children.Add(line);
            }
        }

        //public Joint ScaleTo(Joint joint, double width, double height, float skeletonMaxX, float skeletonMaxY)
        //{
        //    joint.Position = new CameraSpacePoint
        //    {
        //        X = Scale(width, skeletonMaxX, joint.Position.X),
        //        Y = Scale(height, skeletonMaxY, -joint.Position.Y),
        //        Z = joint.Position.Z
        //    };

        //    return joint;
        //}

        //public Joint ScaleTo(Joint joint, double width, double height)
        //{
        //    float x_scale = (float)(1920.0 / width);
        //    float y_scale = (float)(1080.0 / height);
        //    return ScaleTo(joint, width, height, x_scale, y_scale);

        //    //Joint scaled_joint = new Joint();
        //    //scaled_joint.Position = new CameraSpacePoint
        //    //{
        //    //     X = (float) (joint.Position.X * width) / 1920,
        //    //     Y = (float) (joint.Position.Y * height) / 1080,
        //    //     Z = joint.Position.Z
        //    //};

        //    //return scaled_joint;
        //}

        //private float Scale(double maxPixel, double maxSkeleton, float position)
        //{
        //    float value = (float)((((maxPixel / maxSkeleton) / 2) * position) + (maxPixel / 2));

        //    if (value > maxPixel)
        //    {
        //        return (float)maxPixel;
        //    }

        //    if (value < 0)
        //    {
        //        return 0;
        //    }

        //    return value;
        //}

        private List<double> GetLimbAngles(Body body)
        {
            List<double> limb_angles = new List<double>();

            //convert joints to 3Dvectors
            Vector3D Head = ConvertJointtoVector3D(body.Joints[JointType.Head]);
            Vector3D Neck = ConvertJointtoVector3D(body.Joints[JointType.Neck]);
            Vector3D SpineMid = ConvertJointtoVector3D(body.Joints[JointType.SpineMid]);
            Vector3D SpineShoulder = ConvertJointtoVector3D(body.Joints[JointType.SpineShoulder]);
            Vector3D ShoulderLeft = ConvertJointtoVector3D(body.Joints[JointType.ShoulderLeft]);
            Vector3D ShoulderRight = ConvertJointtoVector3D(body.Joints[JointType.ShoulderRight]);
            Vector3D ElbowLeft = ConvertJointtoVector3D(body.Joints[JointType.ElbowLeft]);
            Vector3D ElbowRight = ConvertJointtoVector3D(body.Joints[JointType.ElbowRight]);
            Vector3D WristLeft = ConvertJointtoVector3D(body.Joints[JointType.WristLeft]);
            Vector3D WristRight = ConvertJointtoVector3D(body.Joints[JointType.WristRight]);
            Vector3D HandLeft = ConvertJointtoVector3D(body.Joints[JointType.HandLeft]);
            Vector3D HandRight = ConvertJointtoVector3D(body.Joints[JointType.HandRight]);
            Vector3D SpineBase = ConvertJointtoVector3D(body.Joints[JointType.SpineBase]);
            Vector3D HipLeft = ConvertJointtoVector3D(body.Joints[JointType.HipLeft]);
            Vector3D HipRight = ConvertJointtoVector3D(body.Joints[JointType.HipRight]);
            Vector3D KneeLeft = ConvertJointtoVector3D(body.Joints[JointType.KneeLeft]);
            Vector3D KneeRight = ConvertJointtoVector3D(body.Joints[JointType.KneeRight]);
            Vector3D AnkleLeft = ConvertJointtoVector3D(body.Joints[JointType.AnkleLeft]);
            Vector3D AnkleRight = ConvertJointtoVector3D(body.Joints[JointType.AnkleRight]);
            Vector3D FootLeft = ConvertJointtoVector3D(body.Joints[JointType.FootLeft]);
            Vector3D FootRight = ConvertJointtoVector3D(body.Joints[JointType.FootRight]);

            //angle between
            //neck and shoulders
            double angleNeckShoulderRight = AngleBetweenTwoVectors(SpineShoulder - ShoulderRight, SpineShoulder - Neck);
            double angleNeckShoulderLeft = AngleBetweenTwoVectors(SpineShoulder - ShoulderLeft, SpineShoulder - Neck);

            //elbows
            double angleShoulderRightHandRight = AngleBetweenTwoVectors(ElbowRight - ShoulderRight, ElbowRight - HandRight);
            double angleShoulderLeftHandLeft = AngleBetweenTwoVectors(ElbowLeft - ShoulderLeft, ElbowLeft - HandLeft);

            //knees
            double angleHipRightAnkleRight = AngleBetweenTwoVectors(KneeRight - HipRight, KneeRight - AnkleRight);
            double angleHipLeftAnkleLeft = AngleBetweenTwoVectors(KneeLeft - HipLeft, KneeLeft - AnkleLeft);

            //ankles
            double angleKneeRightFootRight = AngleBetweenTwoVectors(AnkleRight - KneeRight, AnkleRight - FootRight);
            double angleKneeLeftFootLeft = AngleBetweenTwoVectors(AnkleLeft - KneeLeft, AnkleLeft - FootLeft);

            //sides
            double angleSpineMidKneeRight = AngleBetweenTwoVectors(SpineBase - SpineMid, SpineBase - KneeRight);
            double angleSpineMidKneeLeft = AngleBetweenTwoVectors(SpineBase - SpineMid, SpineBase - KneeLeft);

            //armpits
            double angleElbowRightSpineMid = AngleBetweenTwoVectors(ShoulderRight - ElbowRight, ShoulderRight - SpineMid);
            double angleElbowLeftSpineMid = AngleBetweenTwoVectors(ShoulderLeft - ElbowLeft, ShoulderLeft - SpineMid);

            //wrists
            double angleElbowRightHandRight = AngleBetweenTwoVectors(WristRight - ElbowRight, WristRight - HandRight);
            double angleElbowLeftHandLeft = AngleBetweenTwoVectors(WristLeft - ElbowLeft, WristLeft - HandLeft);

            //between legs
            double angleSpineBaseKnees = AngleBetweenTwoVectors(SpineBase - KneeLeft, SpineBase - KneeRight);

            //add to list
            limb_angles.Add(angleNeckShoulderRight);
            limb_angles.Add(angleNeckShoulderLeft);
            limb_angles.Add(angleShoulderRightHandRight);
            limb_angles.Add(angleShoulderLeftHandLeft);
            limb_angles.Add(angleHipRightAnkleRight);
            limb_angles.Add(angleHipLeftAnkleLeft);
            limb_angles.Add(angleKneeRightFootRight);
            limb_angles.Add(angleKneeLeftFootLeft);
            limb_angles.Add(angleSpineMidKneeRight);
            limb_angles.Add(angleSpineMidKneeLeft);
            limb_angles.Add(angleElbowRightSpineMid);
            limb_angles.Add(angleElbowLeftSpineMid);
            limb_angles.Add(angleElbowRightHandRight);
            limb_angles.Add(angleElbowLeftHandLeft);
            limb_angles.Add(angleSpineBaseKnees);

            return limb_angles;
        }

        private Vector3D ConvertJointtoVector3D (Joint joint)
        {
           return new Vector3D(joint.Position.X, joint.Position.Y, joint.Position.Z);
        }

        enum CameraMode
        {
            Color,
            Depth,
            Infrared
        }

        public double AngleBetweenTwoVectors(Vector3D vectorA, Vector3D vectorB)
        {
            double dotProduct = 0.0;
            vectorA.Normalize();
            vectorB.Normalize();
            dotProduct = Vector3D.DotProduct(vectorA, vectorB);

            return (double)Math.Acos(dotProduct) / Math.PI * 180;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //toggle skeleton
            if (showVideo == true)
            {
                showSkeleton = !(showSkeleton);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
            }
        } 

        private void OSC_Send_Limbs(Body body)
        {
            //VVVV.Utils.OSC.OSCMessage bundle = CreateJointBundle(body);
            VVVV.Utils.OSC.OSCMessage bundle = CreateLimbBundle(body);

            if (hearAudio == true)
            {
                VVVV.Utils.OSC.OSCTransmitter transmitter = new VVVV.Utils.OSC.OSCTransmitter("127.0.0.1", Port);
                transmitter.Send(bundle);
                transmitter.Close();
            }
        }

        private VVVV.Utils.OSC.OSCMessage CreateJointBundle(Body body)
        {
            VVVV.Utils.OSC.OSCMessage nestedMessage = new VVVV.Utils.OSC.OSCMessage(TestMethod + "/joints");

            foreach (Joint joint in body.Joints.Values)
            {
                nestedMessage.Append(joint.Position.X);
                nestedMessage.Append(joint.Position.Y);
                nestedMessage.Append(joint.Position.Z);
            }

            return nestedMessage;
        }

        private VVVV.Utils.OSC.OSCMessage CreateLimbBundle(Body body)
        {
            ComboBoxItem ComboItem = (ComboBoxItem)cmbox_dances.SelectedItem;
            string dance_filename=ComboItem.Content.ToString().ToLower().Replace(" ", "");

            VVVV.Utils.OSC.OSCMessage nestedMessage = new VVVV.Utils.OSC.OSCMessage(TestMethod + "/limbs");

            //get the different limbs
            List<double> limb_angles = GetLimbAngles(body);

            if (mode == 'T' & writeToFile == true)
                WriteLimbAnglesToFile(limb_angles, dance_filename);
            else
                if (mode == 'S' & mediaPlayerIsPlaying==true)
                    student_limb_angles.Add(limb_angles);
                    
            foreach (double limb in limb_angles)
                nestedMessage.Append((float)limb);

            return nestedMessage;
        }

        private int calculateAccuracy(string dance_filename)
        {
            double score = 0;
            int accept = 0;
            int count = 0;
            double tolerance = 15.0;
            
            List<List<double>> teacher_limb_angles = ReadLimbAngles(dance_filename);

            int end = Math.Min(teacher_limb_angles.Count, student_limb_angles.Count);

            for(int i = 0;i<end;i++)
            {
                for(int j = 0; j<15;j++)
                {
                    count += 1;

                    var difference = Math.Abs(student_limb_angles[i][j] - teacher_limb_angles[i][j]);

                    if (difference < tolerance) //within 15 degrees
                        accept += 1;
                }
            }

            if (count == 0)
                return 0;

            score = ((double)accept / (double) count) * 100;
            return (int) score;
        }

        private void Mode_Click(object sender, RoutedEventArgs e)
        {
            if (sender == btn_TeacherMode)
            {
                //make other buttons invisible
                mode = 'T';
                btn_Dance1.Visibility = System.Windows.Visibility.Hidden;
                btn_Dance2.Visibility = System.Windows.Visibility.Hidden;
                btn_Dance3.Visibility = System.Windows.Visibility.Hidden;
                btn_Dance4.Visibility = System.Windows.Visibility.Hidden;
                cbox_Video.Visibility = System.Windows.Visibility.Hidden;
                cbox_Audio.Visibility = System.Windows.Visibility.Hidden;
                videoGrid.Visibility = System.Windows.Visibility.Hidden;
                lbl_Score.Visibility = System.Windows.Visibility.Hidden;
                btn_TeacherRecord.Visibility = System.Windows.Visibility.Visible;
                cmbox_dances.Visibility = System.Windows.Visibility.Visible;
                cbox_Video.IsChecked = true;
                cbox_Audio.IsChecked = true;
                canvas.Background = Brushes.Transparent;
                camera.Visibility = System.Windows.Visibility.Visible;
                mediaPlayerIsPlaying = false;
            }
            else
                if(sender == btn_StudentMode)
                {
                    //make buttons visible
                    mode = 'S';
                    btn_Dance1.Visibility = System.Windows.Visibility.Visible;
                    btn_Dance2.Visibility = System.Windows.Visibility.Visible;
                    btn_Dance3.Visibility = System.Windows.Visibility.Visible;
                    btn_Dance4.Visibility = System.Windows.Visibility.Visible;
                    cbox_Video.Visibility = System.Windows.Visibility.Visible;
                    cbox_Audio.Visibility = System.Windows.Visibility.Visible;
                    videoGrid.Visibility = System.Windows.Visibility.Visible;
                    lbl_Score.Visibility = System.Windows.Visibility.Visible;
                    btn_TeacherRecord.Visibility = System.Windows.Visibility.Hidden;
                    cmbox_dances.Visibility = System.Windows.Visibility.Hidden;
                }
        }

        private void Dance_Click(object sender, RoutedEventArgs e)
        {
            if (sender == btn_Dance1)
            {
                //display video of Dance 1
                Uri uvideo = new Uri(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\Videos\dance1.mp4");
                video.Source = uvideo;
                lbl_DanceName.Content = "Dance 1";
            }
            if (sender == btn_Dance2)
            {
                //display video of Dance 2
                Uri uvideo = new Uri(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\Videos\dance2.mp4");
                video.Source = uvideo;
                lbl_DanceName.Content = "Dance 2";
            }
            if (sender == btn_Dance3)
            {
                //display video of Dance 3
                Uri uvideo = new Uri(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\Videos\dance3.mp4");
                video.Source = uvideo;
                lbl_DanceName.Content = "Dance 3";
            }
            if (sender == btn_Dance4)
            {
                //display video of Dance 4
                Uri uvideo = new Uri(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\Videos\dance4.mp4");
                video.Source = uvideo;
                lbl_DanceName.Content = "Dance 4";
            }

            lbl_Score.Content = "Score: -";
        }

        private void cbox_click(object sender, RoutedEventArgs e)
        {
            if (sender == cbox_Audio)
            {
                hearAudio = cbox_Audio.IsChecked.Value;
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
            }
            else
                if (sender == cbox_Video)
                {
                    showVideo = cbox_Video.IsChecked.Value;
                    showSkeleton = cbox_Video.IsChecked.Value;

                    if (showVideo == true)
                    {
                        lbl_Score.Foreground = Brushes.Black;
                        canvas.Background = Brushes.Transparent;
                        camera.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        lbl_Score.Foreground = Brushes.White;
                        canvas.Background = Brushes.Black;
                        camera.Visibility = System.Windows.Visibility.Hidden;
                    }

                    _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
                }
        }

        private void sliProgress_DragStarted(object sender, DragStartedEventArgs e)
        {
            userIsDraggingSlider = true;
        }

        private void sliProgress_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            userIsDraggingSlider = false;
            video.Position = TimeSpan.FromSeconds(sliProgress.Value);
        }
    }
}
