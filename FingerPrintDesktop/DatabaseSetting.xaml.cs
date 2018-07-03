using CommonLib.DAO;
using System;
using System.Windows;

namespace FingerPrintDesktop
{
    
    public partial class DatabaseSetting : Window
    {
        public DatabaseSetting()
        {
            InitializeComponent();
            var db = new DataAccess();
            ConnectionString connectionString = db.GetConnectionStringFromFile();
            if(connectionString != null)
            {
                txtDatabase.Text = connectionString.DatabaseName;
                txtPassword.Password = connectionString.Password;
                txtServer.Text = connectionString.Server;
                txtUsername.Text = connectionString.Username;
                chkOpenMRSDB.IsChecked = connectionString.IsOpenMRSDB;
            }
        }

        private void BtnRegistrationpage_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mw = new MainWindow();
            mw.Show();
            this.Close();
        }

        private void BtnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConnectionString connectionString = new ConnectionString
                {
                    DatabaseName = txtDatabase.Text,
                    Password = txtPassword.Password,
                    Username = txtUsername.Text,
                    Server = txtServer.Text,
                    IsOpenMRSDB = chkOpenMRSDB.IsChecked.Value
                };
                var db = new DataAccess(connectionString.FullConnectionString);
                db.ExecuteScalar(string.Format("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '{0}';", connectionString.DatabaseName));
                MessageBox.Show("Successfully connected to "+txtDatabase.Text);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }

        private void BtnSaveConnection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConnectionString connectionString = new ConnectionString
                {
                    DatabaseName = txtDatabase.Text,
                    Password = txtPassword.Password,
                    Username = txtUsername.Text,
                    Server = txtServer.Text,
                    IsOpenMRSDB = chkOpenMRSDB.IsChecked.Value
                };
                var db = new DataAccess(connectionString.FullConnectionString);
                if (chkOpenMRSDB.IsChecked.Value)
                {
                    db.ExecuteQuery(
                   @"CREATE TABLE IF NOT EXISTS `biometricInfo` (
                        `biometricInfo_Id` INT(11) NOT NULL AUTO_INCREMENT,
                          `patient_Id` INT(11) NOT NULL,
                          `template` TEXT NOT NULL,
                          `imageWidth` INT(11) DEFAULT NULL,
                          `imageHeight` INT(11) DEFAULT NULL,
                          `imageDPI` INT(11) DEFAULT NULL,
                          `imageQuality` INT(11) DEFAULT NULL,
                          `fingerPosition` VARCHAR(50) DEFAULT NULL,
                          `serialNumber` VARCHAR(255) DEFAULT NULL,
                          `model` VARCHAR(255) DEFAULT NULL,
                          `manufacturer` VARCHAR(255) DEFAULT NULL,
                          `creator` INT(11) DEFAULT NULL,
                          `date_created` DATETIME DEFAULT NULL,
                          PRIMARY KEY(`biometricInfo_Id`),
                          FOREIGN KEY(patient_Id) REFERENCES patient(patient_Id),
                          FOREIGN KEY(creator) REFERENCES patient(creator)
                        ) ENGINE = MYISAM AUTO_INCREMENT = 2 DEFAULT CHARSET = utf8; "
                    );
                }
               
                

                db.WriteConnectionToFile(connectionString);
                MessageBox.Show("Save Successfully");
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            } 
        } 
    }
}
