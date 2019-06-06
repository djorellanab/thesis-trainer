using Microsoft.Kinect;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using thesis_trainer.models;
using thesis_trainer.views;
using WinForms = System.Windows.Forms;

namespace thesis_trainer
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged, IDisposable
    {
        #region Propiedades

        private BackgroundWorker _bgWorker;

        private int workerState = 0;
        public int WorkerState
        {
            get { return workerState; }
            set
            {
                workerState = value;
                if (PropertyChanged !=  null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("WorkerState"));
                }
            }
        }

        /// <summary>
        /// Indice que define a la persona que esta escaneando (Vector body)
        /// </summary>
        private int activeBodyIndex = 0;

        /// <summary>
        /// Reconocedor de cuerpos del kinect (6 personas)
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// Lector de frames (Imagenes) del cuerpo
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Temporizador de actualizacion de los frames de kinect cada 60 fps
        /// </summary>
        private DispatcherTimer dispatcherTimer = null;

        /// <summary>
        /// Detector de posturas 
        /// </summary>
        private GestureDetector gestureDetector = null;

        /// <summary>
        /// Objeto que almacena y actualiza los datos a la interfaz de usuario
        /// </summary>
        private GestureResultView gestureResultView = null;

        /// <summary>
        /// Dibuja el cuerpo en la interfaz de usuario
        /// </summary>
        private KinectBodyView kinectBodyView = null;

        /// <summary>
        /// Activa el sensor Kinect
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Define el estado que se encuentra el kinect
        /// </summary>
        private string statusText = null;

        private float timeStep = 0;

        /// <summary>
        /// Obtiene el estado del Kinect en la interfaz de usuario
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            private set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Evento que permie que las propiedades sean cambiados por valores nuevos
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public List<Thread> screenshoots = null;

        HeaderView headerView = null;
        #endregion

        #region constructor

        public MainWindow()
        {
            InitializeComponent();

            this.headerView = new HeaderView();
        }


        #endregion

        #region metodos

        public void DisposeAll()
        {
            // Elimino el evento de renderizacion
            CompositionTarget.Rendering -= this.DispatcherTimer_Tick;

            // Verifico si el reloj esta activo
            if (this.dispatcherTimer != null)
            {
                // Apago el reloj
                this.dispatcherTimer.Stop();
                // Elimino el evento por cada accion del reloj (tick)
                this.dispatcherTimer.Tick -= this.DispatcherTimer_Tick;
            }

            // Veriifico si el lector del cuerpo esta activo
            if (this.bodyFrameReader != null)
            {
                // Elimino todos los datos del lector del cuerpo
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }
            // Limpieza del detector
            if (this.gestureDetector != null)
            {
                // The GestureDetector contains disposable members (VisualGestureBuilderFrameSource and VisualGestureBuilderFrameReader)
                this.gestureDetector.Dispose();
                this.gestureDetector = null;
            }

            // Verifico que el kinect esta utlizando
            if (this.kinectSensor != null)
            {
                // Apago el kinect
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }
     
        /// <summary>
        /// metodo que limpia todos los objetos respectivos
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Limpia el detector de gesturas
        /// </summary>
        /// <param name="disposing">Verdadero, si esta llamando directamente el emtodo</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.gestureDetector != null)
                {
                    this.gestureDetector.Dispose();
                    this.gestureDetector = null;
                }
            }
        }
        /// <summary>
        /// Notifica al interfaz de usuario que esta propieda ha cambiado
        /// </summary>
        /// <param name="propertyName"></param>
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        private void checkData(int step)
        {
            if (step > -1)
            {
                try
                {
                    List<DetailsOfStepFunctionalMovement> details = new List<DetailsOfStepFunctionalMovement>();
                    foreach (KeyValuePair<JointType, DetailsOfStepFunctionalMovement> mean in this.kinectBodyView.joinsAnalize)
                    {
                        DetailsOfStepFunctionalMovement detail = mean.Value.clone();
                        details.Add(detail);
                    }
                    this.kinectBodyView.clearJoins();
                    StepFunctionalMovement sfm = StepFunctionalMovement.createStep(details, step,
                        this.headerView.Trainer.functionalMovement._ID, this.timeStep);
                    this.gestureResultView.addStepDetail(sfm);

                    Thread thread = new Thread(() => screenshot(ref sfm));
                    thread.Start();
                    this.screenshoots.Add(thread);

                }
                catch (Exception)
                { }
            }
        }

        private void screenshot(ref StepFunctionalMovement sfm )
        {
            try
            {
                RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)this.kinectBodyView.ImageSource.Width,
                    (int)this.kinectBodyView.ImageSource.Width, 96, 96, PixelFormats.Pbgra32);
                renderTargetBitmap.Render(this.kinectBodyViewbox);

                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
                string pathPic = $"{this.headerView.Trainer.folder}/{Guid.NewGuid().ToString()}.png";
                FileStream fs = new FileStream(pathPic, FileMode.Create);
                encoder.Save(fs);
                fs.Close();
                sfm.pathImage = pathPic;
            }
            catch (Exception)
            {
            }
        }


        public void startElementToKinect()
        {
            this.screenshoots = new List<Thread>();

            // Obtiene el sensor del kinect (Solo uno)
            this.kinectSensor = KinectSensor.GetDefault();

            // Abre el sensor
            this.kinectSensor.Open();

            // Actualiza el estado del kinect
            this.UpdateKinectStatusText();

            // Obtiene los frames respectivo del kinect
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // Objeto que renderiza el cuerpo en la interfaz de usuario
            this.kinectBodyView = new KinectBodyView(this.kinectSensor, this.headerView);

            // Instancia el objeto con valores por defecto
            this.gestureResultView = new GestureResultView(false, -1.0f, this.headerView.Trainer.functionalMovement.steps.Count);

            // Herramienta que detecta las posturas Con el sensor kinect 
            this.gestureDetector = new GestureDetector(this.kinectSensor, this.gestureResultView, this.headerView.Trainer.folder);

            // Asigna los datos a la interfaz
            this.kinectBodyViewbox.DataContext = this.kinectBodyView;
            this.gestureResultGrid.DataContext = this.gestureResultView;
        }

        /// <summary>
        /// Recupera los datos reciente del kinect 
        /// </summary>
        private void UpdateKinectFrameData()
        {
            // Variable local para verificar si ha recibido un dato
            bool dataReceived = false;

            // Obtiene los ultimos datos (Frames)
            using (var bodyFrame = this.bodyFrameReader.AcquireLatestFrame())
            {
                // Detecta si hay un cuerpo
                if (bodyFrame != null)
                {
                    // Si no existe un cuerpo
                    if (this.bodies == null)
                    {
                        // Crea el arreglo de los cuerpo correspondiente (6 Cuerpos)
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // La primera vez se refresca los datos y le asigna un cuerpo al vector, si solo si no se encuentra nulos los objetos (Reutilizables)
                    bodyFrame.GetAndRefreshBodyData(this.bodies);

                    // Verifica el cuerpo correspondiente
                    if (!this.bodies[this.activeBodyIndex].IsTracked)
                    {
                        // Se ha identificado el cuerpo, por lo cual le asigna el analisis de otro cuerpo
                        int bodyIndex = this.GetActiveBodyIndex();

                        // Verifica que se ha obtenido un cuerpo analizable
                        if (bodyIndex > 0)
                        {
                            // Actualiza el indice
                            this.activeBodyIndex = bodyIndex;
                        }
                    }
                    // Bandera que le indica que se recibio un dato
                    dataReceived = true;
                }
            }

            // Verifica que hay un dato recibido
            if (dataReceived)
            {
                // Obtiene el cuerpo a analizar
                Body activeBody = this.bodies[this.activeBodyIndex];
                // actualiza el nuevo cuerpo
                this.kinectBodyView.UpdateBodyData(activeBody);

                // Verifca nueva postura
                if (activeBody.TrackingId != this.gestureDetector.TrackingId)
                {
                    // Actualiza nueva postura
                    this.gestureDetector.TrackingId = activeBody.TrackingId;
                }

                // Sin postura detectada
                if (this.gestureDetector.TrackingId == 0)
                {
                    // Pausa el detector y coloca los valores por defecto
                    this.gestureDetector.IsPaused = true;
                    this.gestureResultView.UpdateGestureResult(false, -1.0f);
                }
                else // Con postura detectada
                {
                    // Activa el detector
                    this.gestureDetector.IsPaused = false;
                }

                // Actualiza la interfaz de usuario con las ultimas posturas
                this.gestureDetector.UpdateGestureData();
            }
        }

        /// <summary>
        /// Actualiza el estado del kinect
        /// </summary>
        private void UpdateKinectStatusText()
        {
            // reset the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;
        }

        #endregion

        #region eventos
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = this;
            this.headerGrid.DataContext = this.headerView;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            DisposeAll();
        }

        private void btnCalibrar_Click(object sender, RoutedEventArgs e)
        {
            this.headerView.btnCalibrar_Click();
            startElementToKinect();
            // agrego Evento de renderizacion
            CompositionTarget.Rendering += this.DispatcherTimer_Tick;

            // Seteo de actualizacion 60 fps
            this.dispatcherTimer = new DispatcherTimer();
            // Por cada frame se actualiza la pantalla
            this.dispatcherTimer.Tick += this.DispatcherTimer_Tick;
            // Defino 60 fps
            this.dispatcherTimer.Interval = TimeSpan.FromSeconds(1 / 60);
            // Comienza el reloj
            this.dispatcherTimer.Start();
        }

        private void btnOpenFileCarpet_Click(object sender, RoutedEventArgs e)
        {
            this.headerView.btnOpenFileCarpet_Click();
        }

        private void btnOpenFileGBD_Click(object sender, RoutedEventArgs e)
        {
            this.headerView.btnOpenFileGBD_Click();
        }

        private void btnOpenFileJSON_Click(object sender, RoutedEventArgs e)
        {
            this.headerView.btnOpenFileJSON_Click();
        }

        private void btnParar_Click(object sender, RoutedEventArgs e)
        {
            DisposeAll();
            this.headerView = HeaderView.clear();
        }


        private void btnPlayTomaDeDatos_Click(object sender, RoutedEventArgs e)
        {
            this.headerView.btnPlayTomaDeDatos_Click();
        }

        private void btnStopTomaDeDatos_Click(object sender, RoutedEventArgs e)
        {
            this.headerView.disableAllButtonsActions();
            chargeLoading();
            DisposeAll();
        }

        /// <summary>
        /// Renderizacion de frames
        /// </summary>
        /// <param name="sender">objeto enviado por el evento</param>
        /// <param name="e">Argumentos del evento</param>
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            timeStep += (1 / 60);
            // Actualiza el estado del kinect
            this.UpdateKinectStatusText();
            this.UpdateKinectFrameData();
            int step = this.headerView.Trainer.functionalMovement.getStep(this.gestureResultView.Progress);
            this.gestureResultView.updateStep(step);
            this.gestureResultView.checkNewMovementFunctional();
             
            if (this.headerView.isGetData && !this.gestureResultView.isNewFunctionalMovement && !this.gestureResultView.isTakeDataOfFunctionalMovement())
            {
                this.checkData(step);
            }
        }

        #endregion

        #region funciones
        private int GetActiveBodyIndex()
        {
            // Variable local que verifica el indice del cuerpo
            int activeBodyIndex = -1;
            // Obtiene del kinect el numero de cuerpos analizado
            int maxBodies = this.kinectSensor.BodyFrameSource.BodyCount;

            // Recorre todos los cuerpo ha analizar
            for (int i = 0; i < maxBodies; ++i)
            {
                // Verifica que el cuerpo actual esta en seguimiento asi mismo verifica que tenga rastreo de manos
                if (this.bodies[i].IsTracked && (this.bodies[i].HandRightState != HandState.NotTracked || this.bodies[i].HandLeftState != HandState.NotTracked))
                {
                    // Finaliza y actualiza el cuerpo
                    activeBodyIndex = i;
                    break;
                }
            }

            // Retorna el indice a analizar
            return activeBodyIndex;
        }

        private void chargeLoading()
        {
            _bgWorker = new BackgroundWorker();
            _bgWorker.DoWork += (s, e) =>
            {
                MessageBox.Show("Esperar un momento, exportando la informacion a la carpeta correspondiente");
                int total = this.screenshoots.Count + this.gestureResultView.stepsByMovement.Count;
                total += 10;
                int load = 0;
                double getLoad = 0;
                for (int i = 0; i < this.screenshoots.Count; i++)
                {
                    Thread th = this.screenshoots[i];
                    while (th.IsAlive) { }
                    load++;
                    getLoad = (load / total) * 100;
                    this.WorkerState = Convert.ToInt32(getLoad);
                }
                for (int i = 0; i < this.gestureResultView.stepsByMovement.Count; i++)
                {
                    this.gestureResultView.getAngle(this.headerView.Trainer.functionalMovement.anglesOfMovement, i);
                    load++;
                    getLoad = (load / total) * 100;
                    this.WorkerState = Convert.ToInt32(getLoad);
                }
                string stringJson = JsonConvert.SerializeObject(this.gestureResultView.stepsByMovement);
                System.IO.File.WriteAllText($"{this.headerView.Trainer.folder}/datos-entrenamiento.json", stringJson);
                this.WorkerState = 100;
                MessageBox.Show($"Se ha exportado toda la informaccion a la carpeta: {this.headerView.Trainer.folder}");
            };
            _bgWorker.RunWorkerAsync();
        }

        #endregion  
    }
}
