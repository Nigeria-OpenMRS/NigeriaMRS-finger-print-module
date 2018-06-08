﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace FingerPrintModule.DAO
{
    public class DataAccess
    {
        private IDbConnection sql_con;
        //private IDbCommand sql_cmd;
        private IDbDataAdapter dbDataAdapter;
        public DataAccess()
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["NigeriaMRS"].ConnectionString;
            sql_con = new MySqlConnection(connectionString);
            //sql_cmd = new OleDbCommand();
        }

        private DataSet DS = new DataSet();
        private DataTable DT = new DataTable();

        private DataTable GetData(string CommandText)
        {
            sql_con.Open();
            dbDataAdapter = new MySqlDataAdapter(CommandText, (MySqlConnection)sql_con);
            DS.Reset();
            dbDataAdapter.Fill(DS);
            DT = DS.Tables[0];
            sql_con.Close();
            return DT;
        }


        //<add name = "NigeriaMRS" connectionString="Server=127.0.0.1;Port=3306;Data Source=server;User Id=root;Password=root;Provider=ADsDSOObject;"/>
        private void ExecuteQuery(string txtQuery)
        {
            sql_con.Open();
            IDbCommand sql_cmd = new MySqlCommand(txtQuery, (MySqlConnection)sql_con);
            //sql_cmd.CommandText = txtQuery; 
            sql_cmd.ExecuteNonQuery();
            sql_con.Close();
        }

        private int ExecuteScalar(string txtQuery)
        {
            sql_con.Open();
            IDbCommand sql_cmd = new MySqlCommand(txtQuery, (MySqlConnection)sql_con);

            int counts = Convert.ToInt32(sql_cmd.ExecuteScalar());
            sql_con.Close();
            return counts;
        }

        public List<FingerPrintInfo> GetPatientBiometricinfo(string patientId = "")
        {
            List<FingerPrintInfo> _list = new List<FingerPrintInfo>();

            string sqlQuery = "SELECT patient_id, template, imageWidth, imageHeight, imageDPI,  imageQuality, fingerPosition, serialNumber, model, manufacturer, date_created, creator FROM biometricInfo";

            if (!string.IsNullOrEmpty(patientId))
            {
                sqlQuery += string.Format("where patient_id = '{0}'", patientId);
            }


            var dt = GetData(sqlQuery);

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



        public void Save(FingerPrintInfo fingerPrint)
        {
            string insertSQL = string.Format("insert into biometricInfo(patient_Id, template, imageWidth, imageHeight, imageDPI,  imageQuality, fingerPosition, serialNumber, model, manufacturer, creator, date_created)");
            insertSQL += string.Format("Values('{0}','{1}',{2},{3},{4},{5},'{6}','{7}','{8}','{9}','{10}',NOW())", fingerPrint.PatienId, fingerPrint.Template, fingerPrint.ImageWidth, fingerPrint.ImageHeight, fingerPrint.ImageDPI, fingerPrint.ImageQuality, fingerPrint.FingerPositions, fingerPrint.SerialNumber, fingerPrint.Model, fingerPrint.Manufacturer, fingerPrint.Creator);
            ExecuteQuery(insertSQL);

        }
    }
}