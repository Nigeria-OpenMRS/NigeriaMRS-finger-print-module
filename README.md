# NigeriaMRS-finger-print-module

This module is a biometric module built to work alongside the NigeriaMRS which is built from OpenMRS. it is a self hosted WEB API that runs as a windows service. The url is on the app.config.
It works with the openmrs database by adding a table for storing the finger print template and other device information.
Run the sql script below to create this table (the patient_id is a foreign key to the person' table. Not to be confused with any program patient Identifier)
###############################################
CREATE TABLE `biometricInfo` (
  `biometricInfo_Id` int(11) NOT NULL AUTO_INCREMENT,
  `patient_Id` int(11) NOT NULL,
  `template` text NOT NULL,
  `imageWidth` int(11) DEFAULT NULL,
  `imageHeight` int(11) DEFAULT NULL,
  `imageDPI` int(11) DEFAULT NULL,
  `imageQuality` int(11) DEFAULT NULL,
  `fingerPosition` varchar(50) DEFAULT NULL,
  `serialNumber` varchar(255) DEFAULT NULL,
  `model` varchar(255) DEFAULT NULL,
  `manufacturer` varchar(255) DEFAULT NULL,
  `creator` int(11) DEFAULT NULL,
  `date_created` datetime DEFAULT NULL,
  PRIMARY KEY (`biometricInfo_Id`),
  FOREIGN KEY (patient_Id) REFERENCES patient(patient_Id), 
  FOREIGN KEY (creator) REFERENCES patient(creator)
) ENGINE=MyISAM AUTO_INCREMENT=2 DEFAULT CHARSET=utf8;
########################################################
