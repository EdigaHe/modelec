﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using GH_IO.Serialization;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using HumanUIBaseApp;
namespace HumanUI
{

    /// <summary>
    /// Dummy wrapper class extending Button so that event switches know which type to address
    /// </summary>
    /// <seealso cref="System.Windows.Controls.Button" />
    public class TrueOnlyButton : Button
    {
        public TrueOnlyButton()
            : base()
        {

        }
    
    }
}
