using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace adifCreate
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Open file dialog for selecting a .TXT file
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.TXT)|*.TXT",
                Title = "Select a TXT File"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                var lines = File.ReadAllLines(filePath);

                // Extract valid data pairs
                var adifData = ExtractAdifData(lines);

                var fileName = DateTime.Now.ToString("yyyyMMdd_HHmm")+ ".adi"; // ファイル名をYYYYMMDD.adif形式で生成
  
                string folderPath = Path.GetDirectoryName(filePath);

                using (StreamWriter writer = new StreamWriter(folderPath+"\\"+fileName))
                {
                    foreach (var entry in adifData)
                    {
                        writer.WriteLine(entry); // エントリを1行ずつ書き込む
                    }
                }
                MessageBox.Show(folderPath + "\\" + fileName+"にADIFデータを書き込みました");
            }
        }



        static List<string> ExtractAdifData(string[] lines)
        {
            var adifEntries = new List<string>();

            // 正規表現パターン
            var generalPattern = new Regex(@"^(\d{6}_\d{6})\s+([\d.]+)\s+(Rx|Tx)\s+FT8\s+[-+]?\d+\s+\d+\.\d+\s+(\d+)\s+(\S+)\s+(\S+)\s+([-+]\d{2}|R[-+]?\d{2})$");
            var parsedLines = lines.Select(line => generalPattern.Match(line)).Where(m => m.Success).ToList();

            // 送信データと受信データを分ける
            var rxData = parsedLines.Where(m => m.Groups[3].Value == "Rx").ToList();
            var txData = parsedLines.Where(m => m.Groups[3].Value == "Tx").ToList();

            string prevString = "";
            foreach (var rx in rxData)
            {
                string rxTime = rx.Groups[1].Value;      // 受信時刻
                string frequency = rx.Groups[2].Value;  // 周波数
                string distance = rx.Groups[4].Value;   // 距離
                string callRx = rx.Groups[5].Value;     // 受信側コールサイン
                string callTx = rx.Groups[6].Value;     // 送信側コールサイン
                string rstSent = rx.Groups[7].Value;    // RST（送信）

                // 同じ距離、コールサインが一致する送信データを探す
                var tx = txData.FirstOrDefault(t =>
                    t.Groups[4].Value == distance &&
                    t.Groups[5].Value == callTx &&
                    t.Groups[6].Value == callRx); ;

                

                if (tx != null)
                {
                    string txTime = tx.Groups[1].Value;
                    string rstRcvd = tx.Groups[7].Value.Replace("R","");

                    string band = DetermineBand(double.Parse(frequency));
                    if (prevString!=callTx + callRx){
                        adifEntries.Add(FormatAdif(callTx, callRx, txTime, rxTime, rstSent, rstRcvd, band,frequency));
                    }
                    
                    prevString= callTx+callRx;
                }
            }

            return adifEntries;
        }

        static string DetermineBand(double frequency)
        {
            if (frequency >= 1.8 && frequency <= 2.0) return "160M";
            if (frequency >= 3.5 && frequency <= 3.9) return "80M";
            if (frequency >= 7.0 && frequency <= 7.2) return "40M";
            if (frequency >= 10.0 && frequency <= 11.0) return "30M";
            if (frequency >= 14.0 && frequency <= 14.35) return "20M";
            if (frequency >= 18.0 && frequency <= 20.0) return "17M";
            if (frequency >= 21.0 && frequency <= 21.45) return "15M";
            if (frequency >= 144.0 && frequency <= 146.0) return "2M";
            if (frequency >= 50.0 && frequency <= 54.0) return "6M";
            if (frequency >= 28.0 && frequency <= 30.0) return "10M";
            if (frequency >= 24.0 && frequency <= 25.0) return "12M";
            return "UNKNOWN";
        }

        static string FormatAdif(string callTx, string callRx, string txTime, string rxTime, string rstSent, string rstRcvd, string band,string freq)
        {
            string qsoDate = "20"+txTime.Substring(0, 6);
            string timeOn = txTime.Substring(7, 4);

            return $"<CALL:{callTx.Length}>{callTx}<QSO_DATE:8>{qsoDate}<TIME_ON:4>{timeOn}<MODE:3>FT8<FREQ:{freq.Length}>{freq}<BAND:3>{band}<RST_SENT:3>{rstSent}<RST_RCVD:3>{rstRcvd}<EOR>";
        }
    }
}
