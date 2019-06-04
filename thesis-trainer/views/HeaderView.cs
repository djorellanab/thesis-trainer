using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WinForms = System.Windows.Forms;
using thesis_trainer.common;
using thesis_trainer.models;

namespace thesis_trainer.views
{
    public sealed class HeaderView : BindableBase
    {

        public bool isGetData = false;
        public Trainer Trainer = null;

        public bool btnOpenFileGBDIsEnable = true;
        public bool BtnOpenFileGBDIsEnable
        {
            get
            {
                return this.btnOpenFileGBDIsEnable;
            }
            private set
            {
                this.SetProperty(ref this.btnOpenFileGBDIsEnable, value);
            }
        }
        public string btnOpenFileGBDText = "No se ha seleccionado archivo Base de datos de gesturas";
        public string BtnOpenFileGBDText
        {
            get
            {
                return this.btnOpenFileGBDText;
            }
            private set
            {
                this.SetProperty(ref this.btnOpenFileGBDText, value);
            }
        }

        public bool btnOpenFileJSONIsEnable = true;
        public bool BtnOpenFileJSONIsEnable
        {
            get
            {
                return this.btnOpenFileJSONIsEnable;
            }
            private set
            {
                this.SetProperty(ref this.btnOpenFileJSONIsEnable, value);
            }
        }
        public string btnOpenFileJSONText = "No se ha seleccionado archivo metadata";
        public string BtnOpenFileJSONText
        {
            get
            {
                return this.btnOpenFileJSONText;
            }
            private set
            {
                this.SetProperty(ref this.btnOpenFileJSONText, value);
            }
        }

        public bool btnOpenCarpetIsEnable = true;
        public bool BtnOpenCarpetIsEnable
        {
            get
            {
                return this.btnOpenCarpetIsEnable;
            }
            private set
            {
                this.SetProperty(ref this.btnOpenCarpetIsEnable, value);
            }
        }
        public string btnOpenCarpetText = "No se ha seleccionado la carpeta de entrenamiento";
        public string BtnOpenCarpetText
        {
            get
            {
                return this.btnOpenCarpetText;
            }
            private set
            {
                this.SetProperty(ref this.btnOpenCarpetText, value);
            }
        }

        public bool btnCalibrarIsEnable = false;
        public bool BtnCalibrarIsEnable
        {
            get
            {
                return this.btnCalibrarIsEnable;
            }
            private set
            {
                this.SetProperty(ref this.btnCalibrarIsEnable, value);
            }
        }

        public bool btnPararIsEnable = false;
        public bool BtnPararIsEnable
        {
            get
            {
                return this.btnPararIsEnable;
            }
            private set
            {
                this.SetProperty(ref this.btnPararIsEnable, value);
            }
        }

        public bool btnPlayTomaDeDatosIsEnable = false;
        public bool BtnPlayTomaDeDatosIsEnable
        {
            get
            {
                return this.btnPlayTomaDeDatosIsEnable;
            }
            private set
            {
                this.SetProperty(ref this.btnPlayTomaDeDatosIsEnable, value);
            }
        }

        public bool btnStopTomaDeDatos = false;
        public bool BtnStopTomaDeDatos
        {
            get
            {
                return this.btnStopTomaDeDatos;
            }
            private set
            {
                this.SetProperty(ref this.btnStopTomaDeDatos, value);
            }
        }

        public string lbAlturaKinectText = "No se tiene informacion de altura";
        public string LbAlturaKinectText
        {
            get
            {
                return this.lbAlturaKinectText;
            }
            private set
            {
                this.SetProperty(ref this.lbAlturaKinectText, value);
            }
        }

        public string lbProfundidadText = "No se tiene informacion de profundidad";
        public string LbProfundidadText
        {
            get
            {
                return this.lbProfundidadText;
            }
            private set
            {
                this.SetProperty(ref this.lbProfundidadText, value);
            }
        }

        private int workerState = 0;
        public int WorkerState
        {
            get { return workerState; }
            private set
            {
                this.SetProperty(ref this.workerState, value);
            }
        }

        private string lbCalibracion = "Calibracion de articulacion";
        public string LbCalibracion
        {
            get { return lbCalibracion; }
            private set
            {
                this.SetProperty(ref this.lbCalibracion, value);
            }
        }
        public HeaderView()
        {
            this.Trainer = new Trainer();
        }

        private void enableAllButtonFiles()
        {
            this.btnOpenCarpetIsEnable = true;
            this.btnOpenFileGBDIsEnable = true;
            this.btnOpenFileJSONIsEnable = true;
        }

        private void disableAllButtonFiles()
        {
            this.btnOpenCarpetIsEnable = false;
            this.btnOpenFileGBDIsEnable = false;
            this.btnOpenFileJSONIsEnable = false;
        }

        private void readMetadata()
        {
            try
            {
                bool read = this.Trainer.getTraining();
                if (read)
                {
                    this.disableAllButtonFiles();
                    string nameCal = DetailsOfStepFunctionalMovement.getNameJoin(this.Trainer.functionalMovement.focusJoin);
                    this.lbCalibracion = $"Calibracion de ${nameCal}";
                    this.lbAlturaKinectText = $"La altura del Kinect es de {this.Trainer.functionalMovement.height} mts";
                }
                this.btnCalibrarIsEnable = read;
            }
            catch (Exception)
            {
                this.btnCalibrarIsEnable = false;
                this.enableAllButtonFiles();
                WinForms.MessageBox.Show("Hubo un error en la carpeta de metadata");
            }
        }

        private void disableAllButtonGetData()
        {
            this.btnPlayTomaDeDatosIsEnable = false;
            this.btnStopTomaDeDatos = false;
        }


        private void enableAllButtonGetData()
        {
            this.btnPlayTomaDeDatosIsEnable = true;
            this.btnStopTomaDeDatos = true;
        }
        public void updateDepth(float depth)
        {
            this.lbProfundidadText = $"rango de profundida: {this.Trainer.functionalMovement.depthMin.ToString("0.00")} <= {depth.ToString("0.00")} <= {this.Trainer.functionalMovement.depthMax.ToString("0.00")} mts";
            if (this.Trainer.functionalMovement.isCalibrate(depth))
            {
                enableAllButtonGetData();
            }
            else
            {
                disableAllButtonGetData();
            }
        }

        private void getOpenFileDialog(ref string label, string filter, Action<string> getData)
        {
            WinForms.OpenFileDialog openFileDialog = new WinForms.OpenFileDialog();
            openFileDialog.Filter = filter;
            if (openFileDialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                label = openFileDialog.FileName;
                getData(openFileDialog.FileName);
                readMetadata();
                disableAllButtonFiles();
            }
        }

        public void btnOpenFileGBD_Click()
        {
            getOpenFileDialog(ref this.btnOpenFileGBDText,  "Gesture base data (*.gbd)|*.gbd",
                new Action<string>(this.Trainer.getGBD));
        }

        public void btnOpenFileJSON_Click()
        {
            getOpenFileDialog(ref this.btnOpenFileJSONText,  "Json files (*.json)|*.json",
                new Action<string>(this.Trainer.getMetadata));
        }

        public void btnOpenFileCarpet_Click()
        {
            using (var fbd = new WinForms.FolderBrowserDialog())
            {
                WinForms.DialogResult result = fbd.ShowDialog();

                if (result == WinForms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    btnOpenCarpetText = fbd.SelectedPath;
                    this.Trainer.getCarpet(fbd.SelectedPath);
                    readMetadata();
                    disableAllButtonFiles();
                }
            }
        }

        public void btnCalibrar_Click()
        {
            this.btnCalibrarIsEnable = false;
            this.btnPararIsEnable = true;
        }

        public void btnPlayTomaDeDatos_Click()
        {
            this.btnPararIsEnable = false;
            this.btnPlayTomaDeDatosIsEnable = false;
            this.btnStopTomaDeDatos = true;
            this.isGetData = true;
        }

        public void disableAllButtonsActions()
        {
            this.isGetData = false;
            this.btnPararIsEnable = false;
            this.btnPlayTomaDeDatosIsEnable = false;
            this.btnCalibrarIsEnable = false;
            this.btnStopTomaDeDatos = false;
        }

        public static HeaderView clear()
        {
            return new HeaderView();
        }
        public void updateUpload(int worker)
        {
            this.workerState = worker;
        }
    }
}
