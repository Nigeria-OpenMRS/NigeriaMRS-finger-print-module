using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;

namespace CommonLib.DAO
{
    public class DataAccess
    {
        private IDbConnection sql_con;
        //private IDbCommand sql_cmd;
        private IDbDataAdapter dbDataAdapter;
        readonly string path = String.Format(@"{0}\connection_string.txt",
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).Replace("file:\\", ""));

        public DataAccess()
        {
            var connectionInfo = GetConnectionStringFromFile();
            string connectionString = connectionInfo != null ? connectionInfo.FullConnectionString : null; // connectionInfo != null ? connectionInfo.FullConnectionString : System.Configuration.ConfigurationManager.ConnectionStrings["NigeriaMRS"].ConnectionString;
            sql_con = new MySqlConnection(connectionString);
        }

        public DataAccess(string connectionString)
        {
            sql_con = new MySqlConnection(connectionString);
        }

        private DataSet DS = new DataSet();
        private DataTable DT = new DataTable();

        private DataTable GetData(IDbCommand CommandText)
        {
            if (sql_con.State != ConnectionState.Open)
                sql_con.Open();

            dbDataAdapter = new MySqlDataAdapter((MySqlCommand)CommandText);
            DS.Reset();
            dbDataAdapter.Fill(DS);
            DT = DS.Tables[0];
            sql_con.Close();
            return DT;
        }

        public string RetrieveSingleRecord(string txtQuery)
        {
            if (sql_con.State != ConnectionState.Open)
                sql_con.Open();

            IDbCommand sql_cmd = new MySqlCommand(txtQuery, (MySqlConnection)sql_con);
            using (var rd = sql_cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    return Convert.ToString(rd[0]);
                    break;
                }
                return null;
            }
            sql_cmd.ExecuteNonQuery();
            sql_con.Close();
        }

        public void ExecuteQuery(string txtQuery)
        {
            if (sql_con.State != ConnectionState.Open)
                sql_con.Open();

            IDbCommand sql_cmd = new MySqlCommand(txtQuery, (MySqlConnection)sql_con);
            sql_cmd.ExecuteNonQuery();
            sql_con.Close();
        }

        public int ExecuteScalar(string txtQuery)
        {
            if (sql_con.State != ConnectionState.Open)
                sql_con.Open();

            IDbCommand sql_cmd = new MySqlCommand(txtQuery, (MySqlConnection)sql_con);

            int counts = Convert.ToInt32(sql_cmd.ExecuteScalar());
            sql_con.Close();
            return counts;
        }

        public List<FingerPrintInfo> GetPatientBiometricinfo(int patientId =0)
        {
            List<FingerPrintInfo> _list = new List<FingerPrintInfo>();

            IDbCommand sql_cmd = new MySqlCommand
            {
                Connection = (MySqlConnection)sql_con
            };

            string sqlQuery = "";

            if (patientId !=0)
            {
                sqlQuery = "SELECT patient_id, template, imageWidth, imageHeight, imageDPI,  imageQuality, " +
                    "fingerPosition, serialNumber, model, manufacturer, date_created, creator " +
                    "FROM biometricInfo where patient_id = @patientuniqueId";
                sql_cmd.CommandText = sqlQuery;
                var param = sql_cmd.CreateParameter();
                param.ParameterName = "@patientuniqueId";
                param.DbType = DbType.Int32;
                param.Value = patientId;
                sql_cmd.Parameters.Add(param);
            }
            else
            {
                sqlQuery = "SELECT patient_id, template, imageWidth, imageHeight, imageDPI,  imageQuality, fingerPosition, serialNumber, model, manufacturer, date_created, creator FROM biometricInfo";
                sql_cmd.CommandText = sqlQuery;
            }

            var dt = GetData(sql_cmd);

            foreach (DataRow dr in dt.Rows)
            {
                try
                {
                    string fposition = dr.Field<string>("fingerPosition");
                    Enum.TryParse(fposition, out FingerPositions position);

                    _list.Add(new FingerPrintInfo
                    {
                        PatienId = dr.Field<int>("patient_id"),
                        Template = dr.Field<string>("template"),
                        ImageWidth = dr.Field<int>("imageWidth"),
                        ImageHeight = dr.Field<int>("imageHeight"),
                        ImageDPI = dr.Field<int>("imageDPI"),
                        ImageQuality = dr.Field<int>("imageQuality"),
                        FingerPositions = position,
                        SerialNumber = dr.Field<string>("serialNumber"),
                        Model = dr.Field<string>("model"),
                        Manufacturer = dr.Field<string>("manufacturer"),

                    });
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            return _list;
        }

        public void WriteConnectionToFile(ConnectionString connectionString)
        {
            using (StreamWriter sw = (File.Exists(path)) ? new StreamWriter(path, false) : File.CreateText(path))
            {
                sw.WriteLine(connectionString.FullConnectionString);
                sw.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(connectionString));
            }
        }

        public ConnectionString GetConnectionStringFromFile()
        {
            ConnectionString connectionString = null;
            if (File.Exists(path))
            {
                var lines = File.ReadLines(path).ToArray();
                connectionString = Newtonsoft.Json.JsonConvert.DeserializeObject<ConnectionString>(lines[1]);
            }
            return connectionString;
        }

        public ResponseModel SaveToDatabase(List<FingerPrintInfo> fingerPrintList)
        {
            try
            {
                var db = new DataAccess();
                foreach (var f in fingerPrintList)
                {
                    db.Save(f);
                }
                return new ResponseModel
                {
                    ErrorMessage = "Saved successfully",
                    IsSuccessful = true
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel
                {
                    ErrorMessage = ex.Message,
                    IsSuccessful = false
                };
            }
        }

        public void Save(FingerPrintInfo fingerPrint)
        {
            string insertSQL = string.Format("insert into biometricInfo(patient_Id, template, imageWidth, imageHeight, imageDPI,  imageQuality, fingerPosition, serialNumber, model, manufacturer, creator, date_created)");
            insertSQL += string.Format("Values('{0}','{1}',{2},{3},{4},{5},'{6}','{7}','{8}','{9}','{10}',NOW())", fingerPrint.PatienId, fingerPrint.Template, fingerPrint.ImageWidth, fingerPrint.ImageHeight, fingerPrint.ImageDPI, fingerPrint.ImageQuality, fingerPrint.FingerPositions, fingerPrint.SerialNumber, fingerPrint.Model, fingerPrint.Manufacturer, fingerPrint.Creator);
            ExecuteQuery(insertSQL);
        }

        public string RetrievePatientNameByUniqueId(string patientuniqueId)
        {
            string sql = string.Format("SELECT CONCAT(given_name,' ',family_name) AS patient_name, pid.patient_id FROM person_name pn " +
                                 "INNER JOIN patient_identifier pid ON pn.person_id=pid.patient_id " +
                                 "WHERE pid.identifier_type=4 AND pid.identifier= @patientuniqueId;");

            if (sql_con.State != ConnectionState.Open)
                sql_con.Open();

            IDbCommand sql_cmd = new MySqlCommand(sql, (MySqlConnection)sql_con);

            var param = sql_cmd.CreateParameter();
            param.ParameterName = "@patientuniqueId";
            param.DbType = DbType.String;
            param.Value = patientuniqueId;
            sql_cmd.Parameters.Add(param);

            using (var rd = sql_cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    return string.Format("{0}|{1}", rd[0], rd[1]);
                    break;
                }
                return null;
            }
            sql_cmd = null;
            sql_con.Close();
        }

        /// <summary>
        /// </summary>
        /// <param name="patientuniqueId">this is a database Id not the unique pepfar nor hospital Id</param>
        /// <returns></returns>
        public string RetrievePatientNameByPatientId(int patientId)
        {
            string sql = string.Format("SELECT CONCAT(given_name,' ',family_name) AS patient_name, pid.identifier FROM person_name pn " +
                "INNER JOIN patient_identifier pid ON pn.person_id = pid.patient_id " +
                "WHERE pid.identifier_type = 4 AND pid.patient_id = @patientId;");

            if (sql_con.State != ConnectionState.Open)
                sql_con.Open();

            IDbCommand sql_cmd = new MySqlCommand(sql, (MySqlConnection)sql_con);

            var param = sql_cmd.CreateParameter();
            param.ParameterName = "@patientId";
            param.DbType = DbType.Int32;
            param.Value = patientId;
            sql_cmd.Parameters.Add(param);

            using (var rd = sql_cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    return string.Format("{0}|{1}", rd[0], rd[1]);
                    break;
                }
                return null;
            }
            sql_cmd = null;
            sql_con.Close();
        }


        public string RetrievePatientIdByUUID(string UUID)
        {
            string sql = string.Format("SELECT CONCAT(given_name,' ',family_name) AS patient_name, p.person_id " +
                "FROM person_name pn INNER JOIN person p ON pn.person_id = p.person_id " +
                "WHERE p.UUID = @UUID;");

            if (sql_con.State != ConnectionState.Open)
                sql_con.Open();

            IDbCommand sql_cmd = new MySqlCommand(sql, (MySqlConnection)sql_con);

            var param = sql_cmd.CreateParameter();
            param.ParameterName = "@UUID";
            param.DbType = DbType.String;
            param.Value = UUID;
            sql_cmd.Parameters.Add(param);

            using (var rd = sql_cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    return string.Format("{0}|{1}", rd[0], rd[1]);
                    break;
                }
                return null;
            }
            sql_cmd = null;
            sql_con.Close();
        }
    }

}

  
