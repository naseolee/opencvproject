using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using opencvproject;

namespace opencvproject
{
    public partial class Form1 : Form
    {
        string save_file_path = "";
        VideoCapture capture = null;
        string full_file_path = "";
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            save_file_path = AppDomain.CurrentDomain.BaseDirectory;
            this.MaximizeBox = false;
            exe_btn.Visible = false;
            label2.Visible = false;
            label3.Visible = false;

        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void open_file_btn_click(object sender, EventArgs e)
        {
            file_name_box.Clear();
            String file_path = null;
            full_file_path = null;
            //openFileDialog의 시작 위치를 C:\\로 설정
            openFileDialog1.InitialDirectory = "C:\\";

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //선택된 파일의 풀 경로를 불러와 저장
                file_path = openFileDialog1.FileName;
                //해당 경로에서 파일명만 추출하여 표시
                file_name_box.Text = file_path.Split('\\')[file_path.Split('\\').Length - 1];
                //btn visible
                if (file_path != null && !file_path.Equals(""))
                {
                    full_file_path = file_path;
                    exe_btn.Visible = true;
                }
            }
        }

        private void cut_img_exe(object sender, EventArgs e)
        {
            //폴더 작성
            string img_directory = @"save_img_folder\" + DateTime.Now.ToString("yyyyMMdd");
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(save_file_path + img_directory);
            if (!di.Exists)
            {
                di.Create();
            }
            ControlBox = false;
            exe_btn.Visible = false;
            open_file_btn.Visible = false;
            label2.Visible = true;
            label3.Visible = true;

            int count = 1;
            int second = 1;
            int fileCount = 0;

            //start,end파일
            Mat start960 = Cv2.ImRead(save_file_path + @"start_end_file\start960.jpg");
            Mat end960 = Cv2.ImRead(save_file_path + @"start_end_file\end960.jpg");
            //파일 붙이기를 위한 그레이스케일변경(채널 변경 대응 불가메모)
            Cv2.CvtColor(start960, start960, ColorConversionCodes.BGR2GRAY);
            Cv2.CvtColor(end960, end960, ColorConversionCodes.BGR2GRAY);
            Mat imgs = new Mat();
            start960.CopyTo(imgs);
            int rowStart = 950;
            int rowEnd = 1080;
            int colStart = 1;
            int colEnd = 1920;
            
            //opencv 시작
            capture = new VideoCapture(full_file_path);
            if (capture.IsOpened() == false)
            {
                return;
            }
            //20210207(30/60frame별 대응)START
            int frame = (int)capture.Fps;
            int x2Frame = frame * 2;
            int sleepTime = (int)Math.Round(1000 / capture.Fps);
            //작업할 프레임 갯수
            //int frameCount = (int)(capture.FrameCount / 60);
            int frameCount = (int)(capture.FrameCount / x2Frame);
            //20210207(30/60frame별 대응)END

            //using (Window window = new Window("capture"))
            using (Mat image = new Mat()) // Frame image buffer
            {
                // When the movie playback reaches end, Mat.data becomes NULL.
                while (true)
                {
                    //count가 프레임카운트보다 작거나 같을 때만 현 진행상황을 라벨 표지
                    if (count <= frameCount)
                    {
                        label3.Text = count + "/" + frameCount;
                    }
                    capture.Read(image); // same as cvQueryFrame
                    if (image.Empty())
                        break;
                    //20210207(30/60frame별 대응)START
                    //프레임 * 2 당 한번 씩 처리
                    // if (second % 60 == 0)
                    
                    if (second % x2Frame == 0) 
                    //20210207(30/60frame별 대응)END
                    {
                        // 저장한 이미지를 불러와서 그레이 작업
                        Mat frameImg = image;
                        Mat grayImg = new Mat();
                        Cv2.CvtColor(frameImg, grayImg, ColorConversionCodes.BGR2GRAY);
                        // 이미지 하단 컷트
                        Mat cutImg = new Mat();
                        //**SubMat설명 1,2 인수 = 세로 범위, 3,4 인수가 가로 범위
                        cutImg = grayImg.SubMat(rowStart, rowEnd, colStart, colEnd);
                        // 이미지 이진화
                        Mat binaryImg = new Mat();
                        Cv2.Threshold(cutImg, binaryImg, 150, 255, ThresholdTypes.Binary);
                        // 이미지 색반전
                        Mat bitNotImg = new Mat();
                        Cv2.BitwiseNot(binaryImg, bitNotImg);
                        // 사이즈 조절
                        Mat resizeImg = new Mat();
                        Cv2.Resize(bitNotImg, resizeImg, new OpenCvSharp.Size(0, 0), 0.5, 0.5);
                        // 이미지 붙이기
                        Mat contactImg = new Mat();
                        Cv2.VConcat(imgs, resizeImg, imgs);


                        //100개 합성당 파일 1개 저장
                        if (count % 100 == 0)
                        {
                            fileCount += 1;
                            Cv2.VConcat(imgs, end960, imgs);
                            Cv2.ImWrite(di + @"\" + fileCount + ".jpg", imgs);
                            start960.CopyTo(imgs);
                        }
                        Cv2.WaitKey(sleepTime);
                        count += 1;
                    }
                    second += 1;
                }
                //더 이상 읽을 화면이 없을 때 마지막 파일 저장.
                fileCount += 1;
                Cv2.VConcat(imgs, end960, imgs);
                Cv2.ImWrite(di + @"\" + fileCount + ".jpg", imgs);
                MessageBox.Show("자막 사진 파일 추출이 완료되었습니다.", "자막 잘라내기 완료!");
            }
            open_file_btn.Visible = true;
            label2.Visible = false;
            label3.Visible = false;
            file_name_box.Text = "";
            ControlBox = true;
            this.MaximizeBox = false;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void file_name(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

    }
}
