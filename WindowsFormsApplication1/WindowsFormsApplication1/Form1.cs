﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Text.RegularExpressions;

using Dicom;
using Dicom.Media;
using Dicom.Imaging;
using Dicom.IO.Buffer;
using System.Collections;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        // Holds the filepaths of the directory we're visualizing.
        private string[] FileList;
        private int currentFile = 0;
        private XDocument xmlDoc = null;
        StreamWriter sw = null;
        private List<string> tagList = new List<string>();
        //private List<string> generalParameters = new List<string>();
        string path;

        public Form1()
        {
            try
            {
                xmlDoc = XDocument.Load("parameters.xml");
            }
            catch (FileNotFoundException e)
            {
                xmlDoc = new XDocument(
                    new XComment("This .xml file will store the parameters that you want anonymized under the names you have given to each set"),
                    new XElement("Root")
                );
            }
            InitializeComponent();
            ePlistBox();
            local_ip_text.Text = LocalIPAddress().ToString();
        }

        /*This button loops through the already created xml file filled with
        * the tags to be anonymized. The list is populated with these specific
        * tags to be anonymized, and we then search through the DICOM file
        * and "remove" any information that corresponds to the information in the
        * list. This "removed" patient information is passed to the crosswalk
        table for later reference */
        private void Anonymize_Click(object sender, EventArgs e)
        {

            /*when an element in the taglist matches a tag in the
            dicom file, remove it from the dataset and add it to the crosswalk for later reference*/
            string filename = Filepath.Text;
            DicomFile file = DicomFile.Open(filename);
            DicomDataset dataSet = file.Dataset;

            path = "C:\\Users\\Tyler\\Documents\\Schoolwork\\Third Year\\COSC 310\\CrosswalkTable.txt";

            int i = 0;


            List<DicomTag> tags = new List<DicomTag>();
            
            foreach (string input in tagList)
            {                     

                foreach (DicomDictionaryEntry thing in DicomDictionary.Default)
                {
                    if (thing.Tag.ToString().Equals(input))
                    {
                        MessageBox.Show("found matching tag for " + thing.Tag.ToString());
                        tags.Add(thing.Tag);
                    }
                }
         
            }

            addPatientInfo(tags, dataSet, file.Dataset.Get<String>(DicomTag.PatientName));
        
            foreach (DicomTag thing in tags)
            {
                MessageBox.Show("anonymizing tag " + thing.ToString());
                dataSet.Remove(thing);
            }

            MessageBox.Show("writing new dicom file");
            DicomFile anon = new DicomFile(dataSet);
            try
            {
                anon.Save(@"anonymized" + i + ".dcm");
            }
            catch (Exception exc)
            {
                MessageBox.Show("Error: " + exc.ToString());
            }
        }
        
        

//Allows the user to open a Dicom File from their computer
private void OpenSingleFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Open DICOM File";
            ofd.Filter = "DICOM|*.dcm";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Filepath.Text = ofd.FileName;
            }
            ofd.Dispose();
        }

        private void existingParameters_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        public void Input_Click(object sender, EventArgs e)
        {

        }

        public XDocument xDoc
        {
            get { return this.xmlDoc; }
        }

        public void ePlistBox()
        {
            this.existingParameters.Items.Clear();

            IEnumerable<XElement> elementos = xmlDoc.Root.Elements();

            foreach (XElement xelem in elementos)
            {
                this.existingParameters.Items.Add(xelem.Name);
            }
        }

        public string RemoveWhiteSpace(string str)
        {
            return new string(str.ToCharArray().Where(c => !Char.IsWhiteSpace(c)).ToArray());
        }

        private void New_Click_1(object sender, EventArgs e)
        {
            NewParameter form = new NewParameter(this, null, null);
            form.Show();
        }

        //sets title to the selected string from the existing parameters box
        private void Submit_Click(object sender, EventArgs e)
        {
            string title = this.existingParameters.SelectedItem.ToString();
            addToTagList(title);
            MessageBox.Show("The selected parameters have been added!");
        }

        private void Delete_Click_1(object sender, EventArgs e)
        {
            string y = this.existingParameters.SelectedItem.ToString();
            XNode x = xmlDoc.Root.Element(y);
            x.Remove();

            xmlDoc.Save("parameters.xml");

            ePlistBox();
        }

        private void Edit_Click_1(object sender, EventArgs e)
        {
            string y = this.existingParameters.SelectedItem.ToString();
            XElement x = xmlDoc.Root.Element(y);
            IEnumerable<XElement> elementos = x.Descendants();

            x.Remove();

            NewParameter form = new NewParameter(this, elementos, y);
            form.Show();
        }

        //Searches through the xml file for the specified title and adds all child nodes to the tagList
        public void addToTagList(string title)
        {
            XElement titleSearch = xmlDoc.Root.Element(title);
            IEnumerable<XElement> children = titleSearch.Descendants();
            foreach (XElement child in children)
            {
                tagList.Add(child.Value.ToString());
            }
        }

        //A general set of parameters for every patient
        //List<string> getGeneralParameters()
        //{
     
            //generalParameters.Add("PatientName");
            //generalParameters.Add("PatientAge");
            //generalParameters.Add("PatientSex");
            //generalParameters.Add("PatientWeight");
            //generalParameters.Add("PatientHeight");
            //generalParameters.Add("ReferringPhysician");
            //generalParameters.Add("Address");
            //generalParameters.Add("PhoneNumber");
            //generalParameters.Add("BloodType");
            //generalParameters.Add("Priority");
            //generalParameters.Add("Status");

            //return generalParameters;
        //}

        List<string> gettagList()
        {
            return tagList;
        }

        //Writes the general set of parameters for every patient to the crosswalk
        //public void addParametersToCrosswalk(List<string> generalParameters)
        //{
            //using (StreamWriter sw = File.AppendText(path))
            //{
                //sw.Write(crosswalkFormatter("ID:", 10) + "|    ");

                //foreach (string item in generalParameters)
                //{
                    //sw.Write(crosswalkFormatter(item + ":", 20) + "|    ");
                //}
            //}
        //}

        //Writes the patient's information to the correct location in the crosswalk table
        public void addPatientInfo(List<DicomTag> tags, DicomDataset dataSet, string name)
        {
            string patientID = getID(name);

            if (!File.Exists(path))
            {
                MessageBox.Show("Crosswalk table is being created");

                using (StreamWriter sw = new StreamWriter(path, true))
                {
                    sw.Write(crosswalkFormatter("ID: " + patientID, 10) + " |   ");

                    foreach (DicomTag tag in tags)
                    {

                        sw.Write(tag.DictionaryEntry.Keyword + ": " + dataSet.Get<string>(tag) + "|   ");

                    }
                    sw.WriteLine();
                }
            }

            else if (File.Exists(path))
            {
                var fileread = File.ReadAllText(path);
                
                //If the patient already exists in the crosswalk, don't add them again
                if (fileread.Contains(patientID))
                {
                    MessageBox.Show("Patient already in crosswalk!");
                }

                else
                {
                    using (StreamWriter sw = new StreamWriter(path, true))
                    {
                        sw.Write(crosswalkFormatter("ID: " + patientID, 10) + " |   ");

                        foreach (DicomTag tag in tags)
                        {

                            sw.Write(tag.DictionaryEntry.Keyword + ": " + dataSet.Get<string>(tag) + "|   ");

                        }
                        sw.WriteLine();
                    }
                    MessageBox.Show("Patient added to crosswalk");
                }
            }
        }

        //Gets the default hash code for each patient; to be used as the patientID in the crosswalk
        public string getID(string name)
        {
            return Math.Abs(name.GetHashCode()).ToString();
        }

        //A helper class used to make the crosswalk table look tidy
        public static string crosswalkFormatter(string value, int fixedlength)
        {
            int length = 0;
            string returnVal = string.Empty;
            try
            {
                length = value.Length;
                returnVal = value;
                for (int i = 0; i < fixedlength - length - 1; i++)
                {
                    returnVal = returnVal + " ";
                }
            }
            catch (Exception e)
            {
                throw;
            }
            return returnVal;
        }

        public void appendToInfo(string text)
        {
            this.informationBox.Text += text;
        }

        // When the "Open" button is pushed
        private void directoryOpen_Click(object sender, EventArgs e)
        {
            // If there's nothing useful then don't try to open the folder.
            if (string.IsNullOrWhiteSpace(this.directoryBox.Text) || !Directory.Exists(this.directoryBox.Text))
            {
                MessageBox.Show(this, "Given directory path was not valid!");
                return;
            }
            // Populate list with filepaths from the given directory.
            FileList = Directory.GetFiles(this.directoryBox.Text, "*.dcm");
            
            // Reset to null if we got no files back.
            if (FileList.Length == 0)
            {
                FileList = null;
            } // Reset and refresh display.
            currentFile = 0;
            updateDisplay();
        }

        // Redirect method routes call to the "open" handler, when "enter" is pressed.
        private void directoryBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                directoryOpen_Click(sender, e);
            }
        }

        private void goLeft_Click(object sender, EventArgs e)
        {
            // Move left and update.
            moveImage(-1);
            updateDisplay();
        }

        private void goRight_Click(object sender, EventArgs e)
        {
            // Move right and update.
            moveImage(1);
            updateDisplay();
        }

        private void moveImage(int stride)
        {
            // Do nothing if we have no filepaths yet.
            if (FileList == null) { return; }

            // Performs wrap-around if we go past the last file.
            currentFile = (currentFile + stride) % FileList.Length;
            if (currentFile < 0)
            {
                // Need to wrap on left
                currentFile = FileList.Length + currentFile;
            }
        }

        private void updateDisplay()
        {
            // Don't display if we have no filepaths yet.
            if (FileList == null) { return; }

            // Change displayed image.
            var newFile = FileList[currentFile];
            var image = new DicomImage(newFile);
            this.imageDisplay.Image = image.RenderImage().AsBitmap();

            // Change filename display.
            this.currentFileLabel.Text = Path.GetFileName(newFile);

            // Change metadata display
            var data = DicomFile.Open(newFile);
            this.informationBox.Clear();
            new DicomDatasetWalker(data.FileMetaInfo).Walk(new DumpWalker(this));
            new DicomDatasetWalker(data.Dataset).Walk(new DumpWalker(this));
        }

        /*
         * The dump walker iterates a Dataset and prints out all of the
         * DICOM tags in a human-readable format. Large data items are truncated
         * and not displayed. Nested tags are handled properly and shown with
         * indentation.
         */
        private class DumpWalker : IDicomDatasetWalker
        {
            int _level = 0;

            public DumpWalker(Form1 form)
            {
                Form = form;
                Level = 0;
            }

            public Form1 Form
            {
                get;
                set;
            }

            public int Level
            {
                get { return _level; }
                set
                {
                    _level = value;
                    Indent = String.Empty;
                    for (int i = 0; i < _level; i++)
                        Indent += "    ";
                }
            }

            private string Indent
            {
                get;
                set;
            }

            public void OnBeginWalk()
            {
            }

            public bool OnElement(DicomElement element)
            {
                var tag = String.Format("{0}{1}  {2}", Indent, element.Tag.ToString().ToUpper(), element.Tag.DictionaryEntry.Name);

                string value = "<large value not displayed>";
                if (element.Length <= 2048)
                    value = String.Join("\\", element.Get<string[]>());

                if (element.ValueRepresentation == DicomVR.UI && element.Count > 0)
                {
                    var uid = element.Get<DicomUID>(0);
                    var name = uid.Name;
                    if (name != "Unknown")
                        value = String.Format("{0} ({1})", value, name);
                }

                Form.appendToInfo(String.Join(
                    " ",
                    tag,
                    element.ValueRepresentation.Code,
                    element.Length.ToString(),
                    value) + "\r\n");
                return true;
            }

            public bool OnBeginSequence(DicomSequence sequence)
            {
                var tag = String.Format("{0}{1}  {2}", Indent, sequence.Tag.ToString().ToUpper(), sequence.Tag.DictionaryEntry.Name);

                Form.appendToInfo(String.Join(" ", tag, "SQ", String.Empty, String.Empty) + "\r\n");

                Level++;
                return true;
            }

            public bool OnBeginSequenceItem(DicomDataset dataset)
            {
                var tag = String.Format("{0}Sequence Item:", Indent);

                Form.appendToInfo(String.Join(" ", tag, String.Empty, String.Empty, String.Empty) + "\r\n");

                Level++;
                return true;
            }

            public bool OnEndSequenceItem()
            {
                Level--;
                return true;
            }

            public bool OnEndSequence()
            {
                Level--;
                return true;
            }

            public bool OnBeginFragment(DicomFragmentSequence fragment)
            {
                var tag = String.Format("{0}{1}  {2}", Indent, fragment.Tag.ToString().ToUpper(), fragment.Tag.DictionaryEntry.Name);

                Form.appendToInfo(String.Join(" ", tag, fragment.ValueRepresentation.Code, String.Empty, String.Empty) + "\r\n");

                Level++;
                return true;
            }

            public bool OnFragmentItem(IByteBuffer item)
            {
                var tag = String.Format("{0}Fragment", Indent);

                Form.appendToInfo(String.Join(" ", tag, String.Empty, item.Size.ToString(), String.Empty) + "\r\n");
                return true;
            }

            public bool OnEndFragment()
            {
                Level--;
                return true;
            }

            public void OnEndWalk()
            {
            }

            public Task<bool> OnFragmentItemAsync(IByteBuffer item)
            {
                return null;
            }

            public Task<bool> OnElementAsync(DicomElement element)
            {
                return null;
            }
        }
        List<string> getList()
        {
            return tagList;
        }

        //finds your IP address
        public IPAddress LocalIPAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }

        //When local ae label is double clicked, label disappears, text box appears and lets you edit text
        private void local_ae_text_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            local_ae_text.Visible = false;
            local_ae_textbox.Visible = true;
            local_ae_textbox.Text = local_ae_text.Text;
        }

        //When target ae text is double clicked, label disappears, text box appears and lets you edit text
        private void target_ae_text_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            target_ae_text.Visible = false;
            target_ae_textbox.Visible = true;
            target_ae_textbox.Text = target_ae_text.Text;
        }
        
        //syncs local value from text box to label
        private void local_ae_textbox_TextChanged(object sender, EventArgs e)
        {
            local_ae_text.Text = local_ae_textbox.Text;
        }

        //syncs target value from text box to label
        private void target_ae_textbox_TextChanged(object sender, EventArgs e)
        {
            target_ae_text.Text = target_ae_textbox.Text;
        }
        
        //when Enter is pressed, local textbox disappears and label appears
        private void local_ae_textbox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                local_ae_textbox.Visible = false;
                local_ae_text.Visible = true;
            }
        }

        //when Enter is pressed, target textbox disappears and label appears
        private void target_ae_textbox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                target_ae_textbox.Visible = false;
                target_ae_text.Visible = true;
            }
        }

        //when textbox is not in focus, textbox disappears and text reappears
        private void local_ae_textbox_Leave(object sender, EventArgs e)
        {
            local_ae_textbox.Visible = false;
            local_ae_text.Visible = true;
        }

        //when textbox is not in focus, textbox disappears and text reappears
        private void target_ae_textbox_Leave(object sender, EventArgs e)
        {
            target_ae_textbox.Visible = false;
            target_ae_text.Visible = true;
        }

        //when target ip text is double clicked, text disappears and textbox appears and lets user edit
        private void target_ip_text_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            target_ip_text.Visible = false;
            target_ip_textbox.Visible = true;
            target_ip_textbox.Text = target_ip_text.Text;
        }

        //syncs target value from text box to label
        private void target_ip_textbox_TextChanged(object sender, EventArgs e)
        {
            target_ip_text.Text = target_ip_textbox.Text;
        }

        //when Enter is pressed, target textbox disappears and label appears
        private void target_ip_textbox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                target_ip_textbox.Visible = false;
                target_ip_text.Visible = true;
            }
        }

        //when textbox is not in focus, textbox disappears and text reappears
        private void target_ip_textbox_Leave(object sender, EventArgs e)
        {
            target_ip_textbox.Visible = false;
            target_ip_text.Visible = true;
        }
        //when user selects '[Custom]', combobox disappears and textbox appears letting the user change the value
        private void local_port_combobox_TextChanged(object sender, EventArgs e)
        {
            if(local_port_combobox.Text == "[Custom]")
            {
                local_port_textbox.Visible = true;
                local_port_combobox.Visible = false;
            }
        }
        //Syncs the value being changed from textbox to combobox
        private void local_port_textbox_TextChanged(object sender, EventArgs e)
        {
            local_port_combobox.Text = local_port_textbox.Text;
        }
        //when user hits Enter, textbox disappears and combobox reappears
        private void local_port_textbox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                local_port_textbox.Visible = false;
                local_port_combobox.Visible = true;
            }
        }
        //when textbox is out of focus, textbox disappears and combobox reappears
        private void local_port_textbox_Leave(object sender, EventArgs e)
        {
            local_port_textbox.Visible = false;
            local_port_combobox.Visible = true;
        }
        //when user selects '[Custom]', combobox disappears and textbox appears letting the user change the value
        private void target_port_combobox_TextChanged(object sender, EventArgs e)
        {
            if (target_port_combobox.Text == "[Custom]")
            {
                target_port_textbox.Visible = true;
                target_port_combobox.Visible = false;
            }
        }
        //Syncs the value being changed from textbox to combobox
        private void target_port_textbox_TextChanged(object sender, EventArgs e)
        {
               target_port_combobox.Text = target_port_textbox.Text;
        }
        //when user hits Enter, textbox disappears and combobox reappears
        private void target_port_textbox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                target_port_textbox.Visible = false;
                target_port_combobox.Visible = true;
            }
        }
        //when textbox is out of focus, textbox disappears and combobox reappears
        private void target_port_textbox_Leave(object sender, EventArgs e)
        {
              target_port_textbox.Visible = false;
              target_port_combobox.Visible = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //catch exception where null text
            Listener list = new Listener(int.Parse(this.local_port_textbox.Text));
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            DicomSender send = new DicomSender(this.target_ip_textbox.Text, int.Parse(this.target_port_textbox.Text), this.target_ae_textbox.Text, this.local_ae_textbox.Text);
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                this.sendFile.Text = openFileDialog1.FileName;
                send.sendDicom(DicomFile.Open(@"" + this.sendFile.Text));

            }
        }

        private void New_Click(object sender, EventArgs e)
        {
            NewParameter form = new NewParameter(this, null, null);
            form.Show();
        }

        private void existingParameters_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }

        private void existingParameters_SelectedIndexChanged_2(object sender, EventArgs e)
        {

        }

        private void Delete_Click(object sender, EventArgs e)
        {
            string y = this.existingParameters.SelectedItem.ToString();
            XNode x = xmlDoc.Root.Element(y);
            x.Remove();

            xmlDoc.Save("parameters.xml");

            ePlistBox();
        }

        private void Edit_Click(object sender, EventArgs e)
        {
            string y = this.existingParameters.SelectedItem.ToString();
            XElement x = xmlDoc.Root.Element(y);
            IEnumerable<XElement> elementos = x.Descendants();

            x.Remove();

            NewParameter form = new NewParameter(this, elementos, y);
            form.Show();
        }

        private void Listen_Click(object sender, EventArgs e)
        {
            Listener list = new Listener();
        }

        private void sendFile_TextChanged(object sender, EventArgs e)
        {

        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            DicomSender send = new DicomSender(this.target_ip_textbox.Text, int.Parse(this.target_port_textbox.Text), this.target_ae_textbox.Text, this.local_ae_textbox.Text);
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                this.sendFile.Text = openFileDialog1.FileName;
                send.sendDicom(DicomFile.Open(@"" + this.sendFile.Text));

            }
        }
    }
}
