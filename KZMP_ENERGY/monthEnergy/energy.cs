using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Windows.Forms;
using System.Threading;

namespace KZMP_ENERGY.monthEnergy
{
    public class energy
    {
        public KZMP_ENERGY.FormPowerProfile powerProfForm;

        public SerialPort portLocal;
        byte mercAddress;
        string monthNumber;

        bool respCrcCheck = false;
        int repeatCounter = 0;

        int timeOver = 500;
        bool timeOverFlag = false;

        public energy(ref SerialPort port,byte mercAddress, string monthNumber, ref KZMP_ENERGY.FormPowerProfile powPrForm)
        {
            this.portLocal = port;
            this.mercAddress = mercAddress;
            this.monthNumber = monthNumber;
            this.powerProfForm = powPrForm;
        }
        public async void getMonthEnergy()
        {
            powerProfForm.richTextBox_conStatus2.AppendText("\n-------------------------------------------------------------------------------------------------\nЧТЕНИЕ ПОКАЗАНИЙ НАКОПЛЕННОЙ ЭНЕРГИИ...\n-------------------------------------------------------------------------------------------------\n");
            powerProfForm.richTextBox_conStatus2.ScrollToCaret();

            string b = "B";
            int nextMonth = 0;
           
            switch (monthNumber)
            {
                case "10": { b = b + "A"; nextMonth = 11; break; }
                case "11": { b = b + "B"; nextMonth = 12; break; }
                case "12": { b = b + "C"; nextMonth = 1; break; }
                default: { b = b + monthNumber; nextMonth = Convert.ToInt32(monthNumber) + 1; break; }
            }

            byte[] request_crc = new byte[4] { mercAddress, 0x05, Convert.ToByte(b, 16), 0x00 };

            byte[] crc = KZMP_ENERGY.FormPowerProfile.CalculateCrc16Modbus(request_crc);

            byte[] request = new byte[6] { mercAddress, 0x05, Convert.ToByte(b, 16), 0x00, crc[0], crc[1] };

            while (!respCrcCheck && repeatCounter < 3)
            {
                await Task.Run(() => write(request, portLocal));

                repeatCounter++;
            }
        }

        public void write(byte[] mes, SerialPort port)
        {
            if (port.CDHolding)
            {
                port.DiscardInBuffer();
                port.Write(mes, 0, mes.Length);

                powerProfForm.richTextBox_conStatus2.AppendText(@"# Запрос на чтение накопленной энергии отправлен");
                powerProfForm.richTextBox_conStatus2.ScrollToCaret();
            }
            else
            {
                MessageBox.Show(
                     "Ошибка связи: потеря несущей.\nБудет выполнена перезагрузка программы.",
                     "kzmp_energy notification",
                     MessageBoxButtons.OK,
                     MessageBoxIcon.Information,
                     MessageBoxDefaultButton.Button1,
                     MessageBoxOptions.DefaultDesktopOnly);

                Application.Restart();
            }
        }
        public void read(SerialPort port)
        {
            Thread.Sleep(500);
            List<byte> msg = new List<byte>();
            int count = 0;
            int lng = 0;
            int timeCountIndex = 500;
            timeOver = 500;
            respCrcCheck = false;

            while(lng < 19)
            {
                if (!port.CDHolding)
                {
                    MessageBox.Show(
                    "Ошибка связи: потеря несущей.\nБудет выполнена перезагрузка программы.",
                    "kzmp_energy notification",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.DefaultDesktopOnly);

                    Application.Restart();
                }
                count = port.BytesToRead;
                if (count != 0)
                {
                    byte[] ByteArray = new byte[count];
                    port.Read(ByteArray, 0, count);
                    foreach (byte f in ByteArray)
                    {
                        msg.Add(f);
                    }
                    lng = msg.Count;
                }
                Thread.Sleep(500);
                timeCountIndex = timeCountIndex + 500;
                timeOver = timeOver + 500;

                if (timeOver > 7000)
                {
                    timeOverFlag = true;

                    break;
                }

                if (lng >= 19)
                {
                    timeOverFlag = false;
                }
            }

            if(!timeOverFlag)
            {
                byte[] check_crc_massa = new byte[] { msg[0], msg[1], msg[2], msg[3], msg[4], msg[5], msg[6], msg[7], msg[8], msg[9], msg[10], msg[11], msg[12], msg[13], msg[14], msg[15], msg[16] };

                byte[] crc_out = KZMP_ENERGY.FormPowerProfile.CalculateCrc16Modbus(check_crc_massa);

                if(crc_out[0]==msg[17] && crc_out[1] == msg[18])
                {
                    respCrcCheck = true;

                    string byte1 = msg[4].ToString("X");
                    string byte2 = msg[3].ToString("X");
                    string byte3 = msg[2].ToString("X");
                    string byte4 = msg[1].ToString("X");

                    

                    powerProfForm.richTextBox_conStatus2.AppendText(@"# Ответ успешно получен и обработан!");
                    powerProfForm.richTextBox_conStatus2.ScrollToCaret();
                }
                else 
                {
                    respCrcCheck = false;
                }
            }
        }
    }
}
