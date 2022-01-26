using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using KZMP_ENERGY.monthEnergy.energyReport;
using System.Configuration;
using System.Data;
using System.Windows.Forms;

namespace KZMP_ENERGY.monthEnergy.energyReport
{
    public class energyReport
    {
        string companyName;
        string periodOfReport;
        string monthOfReport;
        string monthOfReportName;
        string nextMonthOfReport;
        string yearOfReport;
        string nextYearOfReport;
        string companyInn;

        string connectionString;

        List<meterInfo> metersList = new List<meterInfo>();
        SqlConnection connection;
        SqlCommand cmd;
        public energyReport(string companyName, string month,string year, string companyInn)
        {
            this.companyName = companyName;
            this.companyInn = companyInn;

            this.monthOfReport = month;
            this.yearOfReport = year;

            switch(month)
            {
                case "12": { 
                        nextMonthOfReport = "1";
                        nextYearOfReport=Convert.ToString(Convert.ToInt32(year)+1); 
                        break; }
                default:
                    {
                        nextMonthOfReport = Convert.ToString(Convert.ToInt32(month) + 1);
                        nextYearOfReport = "";
                        break;
                    }
            }

            switch(month)
            {
                case "1": { monthOfReportName = "Январь";break; }
                case "2": { monthOfReportName = "Февраль"; break; }
                case "3": { monthOfReportName = "Март"; break; }
                case "4": { monthOfReportName = "Апрель"; break; }
                case "5": { monthOfReportName = "Май"; break; }
                case "6": { monthOfReportName = "Июнь"; break; }
                case "7": { monthOfReportName = "Июль"; break; }
                case "8": { monthOfReportName = "Август"; break; }
                case "9": { monthOfReportName = "Сентябрь"; break; }
                case "10": { monthOfReportName = "Октябрь"; break; }
                case "11": { monthOfReportName = "Ноябрь"; break; }
                case "12": { monthOfReportName = "Декабрь"; break; }
            }


            connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            connection = new SqlConnection(connectionString);

            
        }

        public void createEnergyReport()
        {
            getMeters();
            //1.Получить общие суммы мощностей для каждого счётчика
            getGeneralPowersSum();
            //2.Получить значения энергий
            getEnergy();
            //3.Сформировать html отчёт 

        }

        public void getMeters()
        {
            try
            {
                string sql_cmd = "select id_meter, type, address,Interface, Transformation_ratio " +
                             "from dbo.meter " +
                             "where INN like '" + companyInn + "';";
                if (!(connection.State == ConnectionState.Open))
                {
                    connection.Open();

                    cmd = new SqlCommand(sql_cmd, connection);
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            meterInfo meterObj = new meterInfo();
                            meterObj.id = Convert.ToString(reader.GetValue(0));
                            meterObj.typeOfmeter = Convert.ToString(reader.GetValue(1));
                            meterObj.addressOfmeter = Convert.ToString(reader.GetValue(2));
                            meterObj.meterInter = Convert.ToString(reader.GetValue(3));
                            meterObj.transRatio = Convert.ToString(reader.GetValue(4));

                            metersList.Add(meterObj);
                        }
                    }
                    reader.Close();
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void getGeneralPowersSum()
        {
            try
            {
                if (!(connection.State == ConnectionState.Open))
                {
                    connection.Open();
                }

                foreach (meterInfo meterObj in metersList)
                {
                    string periodStart = "01." + monthOfReport + "." + yearOfReport;
                    string periodEnd = "01." + nextMonthOfReport + ".";
                    double powerSum = 0;

                    if (nextYearOfReport!="")
                    {
                        periodEnd += nextYearOfReport;
                    }
                    else
                    {
                        periodEnd += yearOfReport;
                    }

                    string sql_cmd = "select Pplus " +
                                     "from dbo.power_profile_m " +
                                     "where date>='"+periodStart+"' and date<'"+periodEnd+"' " +
                                        "and (date!='"+periodStart+"' or time!='00:00:00') and id= "+meterObj.id+";";

                    cmd = new SqlCommand(sql_cmd,connection);

                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            string pPlus = Convert.ToString(reader.GetValue(0));

                            double pPlusDoub = Convert.ToDouble(pPlus);
                            powerSum += pPlusDoub;
                        }
                    }
                    reader.Close();


                    sql_cmd = "select Pplus " +
                              "from dbo.power_profile_m " +
                              "where date='" + periodEnd + "' and time='00:00:00' and id= "+ meterObj.id + ";";
                    cmd = new SqlCommand(sql_cmd, connection);

                    reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            string pPlus = Convert.ToString(reader.GetValue(0));

                            double pPlusDoub = Convert.ToDouble(pPlus);
                            powerSum += pPlusDoub;
                        }
                    }
                    reader.Close();

                    meterObj.powerSum = Convert.ToString(powerSum);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void getEnergy()
        {
            try
            {
                if (!(connection.State == ConnectionState.Open))
                {
                    connection.Open();
                }

                foreach (meterInfo meterObj in metersList)
                {
                    string sql_cmd = "select StartValue, EndValue, Total " +
                                     "from dbo.EnergyTable " +
                                     "where MeterID = " + meterObj.id+
                                     " and Year = "+yearOfReport+" and Month='"+monthOfReportName+"';";

                    cmd = new SqlCommand(sql_cmd, connection);

                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            meterObj.startValue = Convert.ToString(reader.GetValue(0));
                            meterObj.endValue = Convert.ToString(reader.GetValue(1));
                            meterObj.totalValue = Convert.ToString(reader.GetValue(2));
                        }
                    }
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
