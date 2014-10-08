/*
 * Copyright (c) 2014 Microsoft Mobile. All rights reserved.
 * See the license text file provided with this project for more information.
 */

using Windows.UI.Xaml.Documents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using System.Xml;
using System.Xml.Linq;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace ActivitiesExample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AboutPage : Page
    {

        public AboutPage()
        {
            this.InitializeComponent();

            Loaded += AboutPage_Loaded;
        }

        async void AboutPage_Loaded(object sender, RoutedEventArgs e)
        {
            string version = "";

            var uri = new System.Uri("ms-appx:///AppxManifest.xml");
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            using (var rastream = await file.OpenReadAsync())
            using (var appManifestStream = rastream.AsStreamForRead())
            {
                using (var reader = XmlReader.Create(appManifestStream, new XmlReaderSettings { IgnoreWhitespace = true, IgnoreComments = true }))
                {
                    var doc = XDocument.Load(reader);
                    var app = doc.Descendants(doc.Root.Name.Namespace + "Identity").FirstOrDefault();
                    if (app != null)
                    {
                        var versionAttribute = app.Attribute("Version");
                        if (versionAttribute != null)
                        {
                            version = versionAttribute.Value;
                        }
                    }
                }
            }

            VersionNumber.Text = version;
        }

    }
}
