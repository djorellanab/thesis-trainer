using Microsoft.Kinect;
using System.Collections.Generic;
using System.Windows;
using thesis_trainer.common;
using thesis_trainer.models;

namespace thesis_trainer
{
    /// <summary>
    /// Objeto que almacena los valores correspondientes y asi mismo notifica su acgtualizacion
    /// </summary>
    public sealed class GestureResultView : BindableBase
    {
        #region  Atributos
        private int countStep = 0;
        public List<List<StepFunctionalMovement>> stepsByMovement = null;
        private int repetitions = 0;
        public int Repetitions
        {
            get
            {
                return this.repetitions;
            }

            private set
            {
                this.SetProperty(ref this.repetitions, value);
            }
        }
        /// <summary>
        /// Valor continuo que verifica el progreso de levantamiento de mano
        /// </summary>
        private float progress = 0.0f;

        /// <summary> 
        /// Valor de UI
        /// </summary>
        public float Progress
        {
            get
            {
                return this.progress;
            }

            private set
            {
                this.SetProperty(ref this.progress, value);
            }
        }


        /// <summary>
        /// Verifica si el cuerpo actual tiene seguimiento
        /// </summary>
        private bool isTracked = false;

        /// <summary>
        /// Valor que indica si el cuerpo  asociado al detector tiene seguimiento
        /// </summary>
        public bool IsTracked
        {
            get
            {
                return this.isTracked;
            }

            private set
            {
                this.SetProperty(ref this.isTracked, value);
            }
        }

        public int indexStep = 0;
        public int IndexStep
        {
            get
            {
                return this.indexStep;
            }

            private set
            {
                this.SetProperty(ref this.indexStep, value);
            }
        }

        public bool isNewFunctionalMovement = false;
        #endregion

        #region constructor
        /// <summary>
        /// Constructor que inicializa todos los componentes
        /// </summary>
        /// <param name="isTracked">Variable de seguimiento</param>
        /// <param name="progress">Valor de proceso</param>
        public GestureResultView(bool isTracked, float _progress, int _countStep)
        {
            this.progress = _progress;
            this.IsTracked = isTracked;
            this.IndexStep = 0;
            this.stepsByMovement = new List<List<StepFunctionalMovement>>();
            createStepsDetail();
            this.repetitions = 0;
            this.countStep = _countStep;
        }


        /// <summary>
        /// Actualiza los valores desplegado en la interfaza de usuario
        /// </summary>
        /// <param name="isBodyTrackingIdValid">Verifica que el cuerpo tenga seguimiento</param>
        /// <param name="progress">El valor del progreso continuo</param>
        public void UpdateGestureResult(bool isBodyTrackingIdValid, float progress)
        {
            // Actualiza seguimiento
            this.IsTracked = isBodyTrackingIdValid;

            // Si no hay seguimiento, se asigna valores por default
            if (!this.isTracked)
            {
                this.Progress = -1.0f;
            }
            else // Si hay, Se asigna los valores pasado
            {
                this.Progress = progress;
            }
        }

        private bool isCorrectMF()
        {
            foreach (StepFunctionalMovement item in this.stepsByMovement[this.repetitions])
            {
                if (item == null) { return false; }
            }
            return true;
        }

        public void addStepDetail(StepFunctionalMovement step)
        {
            this.stepsByMovement[this.repetitions][step.step] = step;
            if (step.step == (this.stepsByMovement.Count-1) && isCorrectMF())
            {
                isNewFunctionalMovement = true;
                this.createStepsDetail();
                this.Repetitions++;
            }
        }


        private void createStepsDetail()
        {
            List<StepFunctionalMovement> _steps = new List<StepFunctionalMovement>();
            for (int i = 0; i < this.countStep; i++)
            {
                _steps.Add(null);
            }
            this.stepsByMovement.Add(_steps);
        }

        public void getAngle(List<int> _angles, int mf)
        {
            List<StepFunctionalMovement> steps = this.stepsByMovement[mf];
            foreach (StepFunctionalMovement step in steps)
            {
                if (step != null)
                {
                    foreach (int _angle in _angles)
                    {
                        List<JointType> joints = DetailsOfStepFunctionalMovement.translateAngles(_angle);
                        List<DetailsOfStepFunctionalMovement> vectorialPoints = step.detailsOfStepFunctionalMovement.FindAll(x => joints.Contains((JointType)x.join));
                        if (vectorialPoints.Count != 3) { continue; }
                        DetailsOfStepFunctionalMovement origen = vectorialPoints.Find(x => (int)x.angle == (int)joints[0]);
                        if (origen != null) { continue; }
                        vectorialPoints.RemoveAll(x => (int)x.angle == (int)joints[0]);
                        if (vectorialPoints.Count != 2) { continue; }
                        Point po = new Point(origen.x, origen.y);
                        Point p1 = new Point(vectorialPoints[0].x, vectorialPoints[0].y);
                        Point p2 = new Point(vectorialPoints[1].x, vectorialPoints[1].y);
                        origen.angle = KinectAngleBody.getAngleBody(po, p1, p2);
                    }
                }
            }
        }

        public void checkNewMovementFunctional()
        {
            if (this.isNewFunctionalMovement)
            {
                this.isNewFunctionalMovement = (this.indexStep == 0);
            }
        }

        public bool isTakeDataOfFunctionalMovement()
        {
            return this.stepsByMovement[this.repetitions][this.indexStep] != null;
        }

        public void updateStep(int step)
        {
            if (step >-1)
            {
                this.IndexStep = step;
            }
        }
        #endregion
    }
}
