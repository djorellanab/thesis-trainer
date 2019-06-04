﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace thesis_trainer.models
{
    public class StepFunctionalMovement
    {
        public string functionalMovement { get; set; }
        public int step { get; set; }
        public double time { get; set; }
        public bool clasification { get; set; }
        public List<DetailsOfStepFunctionalMovement> detailsOfStepFunctionalMovement { get; set; }
        public string pathImage { get; set; }
        public bool status { get; set; }

        public static StepFunctionalMovement createStep(List<DetailsOfStepFunctionalMovement> details, int _step,
            string _functionalMovement, double _time)
        {
            return new StepFunctionalMovement()
            {
                clasification = false,
                detailsOfStepFunctionalMovement = details,
                functionalMovement = _functionalMovement,
                status = true,
                step = _step,
                time = _time
            };
        }

    }
}
