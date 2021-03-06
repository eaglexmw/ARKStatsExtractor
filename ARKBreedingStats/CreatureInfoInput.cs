﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace ARKBreedingStats
{
    public partial class CreatureInfoInput : UserControl
    {
        public delegate void Add2LibraryClickedEventHandler(CreatureInfoInput sender);
        public event Add2LibraryClickedEventHandler Add2Library_Clicked;
        public delegate void Save2LibraryClickedEventHandler(CreatureInfoInput sender);
        public event Save2LibraryClickedEventHandler Save2Library_Clicked;
        public delegate void RequestParentListEventHandler(CreatureInfoInput sender);
        public event RequestParentListEventHandler ParentListRequested;
        public delegate void RequestCreatureDataEventHandler(CreatureInfoInput sender, bool patternEditor);
        public event RequestCreatureDataEventHandler CreatureDataRequested;
        public bool extractor;
        private Sex sex;
        private CreatureStatus status;
        public bool parentListValid;
        private int speciesIndex;
        public StatIO weightStat;
        private ToolTip tt = new ToolTip();
        private bool mutationManuallyChanged;
        private bool updateMaturation;
        private List<Creature> _females;
        private List<Creature> _males;
        private string[] _ownersTribes;
        public bool ownerLock, tribeLock; // if true the OCR will not change these fields

        public CreatureInfoInput()
        {
            InitializeComponent();
            speciesIndex = -1;
            parentComboBoxMother.naLabel = " - Mother n/a";
            parentComboBoxMother.Items.Add(" - Mother n/a");
            parentComboBoxFather.naLabel = " - Father n/a";
            parentComboBoxFather.Items.Add(" - Father n/a");
            parentComboBoxMother.SelectedIndex = 0;
            parentComboBoxFather.SelectedIndex = 0;
            tt.SetToolTip(buttonSex, "Sex");
            tt.SetToolTip(buttonStatus, "Status");
            tt.SetToolTip(dateTimePickerAdded, "Domesticated at");
            tt.SetToolTip(numericUpDownMutations, "Mutation-Counter");
            tt.SetToolTip(btnGenerateUniqueName, "Generate automatic name\nRight-click to edit the pattern.");
            tt.SetToolTip(lblOwner, "Click to toggle if the OCR can change the owner-field.\nEnable it if the OCR doesn't recognize the owner-name correctly and you want to add multiple creatures with the same owner.");
            tt.SetToolTip(lblTribe, "Click to toggle if the OCR can change the tribe-field.\nEnable it if the OCR doesn't recognize the tribe-name correctly and you want to add multiple creatures with the same tribe.");
            tt.SetToolTip(lblName, "Click to copy the name to the clipboard, e.g. for pasting it in the game.");
            updateMaturation = true;
        }

        private void buttonAdd2Library_Click(object sender, EventArgs e)
        {
            Add2Library_Clicked(this);
        }

        private void buttonSaveChanges_Click(object sender, EventArgs e)
        {
            Save2Library_Clicked(this);
        }

        public string CreatureName
        {
            get { return textBoxName.Text; }
            set { textBoxName.Text = value; }
        }
        public string CreatureOwner
        {
            get { return textBoxOwner.Text; }
            set { textBoxOwner.Text = value; }
        }
        public string CreatureTribe
        {
            get { return textBoxTribe.Text; }
            set { textBoxTribe.Text = value; }
        }
        public Sex CreatureSex
        {
            get { return sex; }
            set
            {
                sex = value;
                buttonSex.Text = Utils.sexSymbol(sex);
                buttonSex.BackColor = Utils.sexColor(sex);
                tt.SetToolTip(buttonSex, "Sex: " + sex.ToString());
                if (sex == Sex.Female)
                    checkBoxNeutered.Text = "Spayed";
                else
                    checkBoxNeutered.Text = "Neutered";
            }
        }
        public CreatureStatus CreatureStatus
        {
            get { return status; }
            set
            {
                status = value;
                buttonStatus.Text = Utils.statusSymbol(status);
                tt.SetToolTip(buttonStatus, "Status: " + status.ToString());
            }
        }
        public string CreatureServer
        {
            get { return cbServer.Text; }
            set { cbServer.Text = value; }
        }
        public Creature mother
        {
            get
            {
                return parentComboBoxMother.SelectedParent;
            }
            set
            {
                parentComboBoxMother.preselectedCreatureGuid = (value == null ? Guid.Empty : value.guid);
            }
        }
        public Creature father
        {
            get
            {
                return parentComboBoxFather.SelectedParent;
            }
            set
            {
                parentComboBoxFather.preselectedCreatureGuid = (value == null ? Guid.Empty : value.guid);
            }
        }
        public string CreatureNote
        {
            get { return textBoxNote.Text; }
            set { textBoxNote.Text = value; }
        }

        private void buttonGender_Click(object sender, EventArgs e)
        {
            CreatureSex = Utils.nextSex(sex);
        }

        private void buttonStatus_Click(object sender, EventArgs e)
        {
            CreatureStatus = Utils.nextStatus(status);
        }

        public List<Creature>[] Parents
        {
            set
            {
                if (value != null)
                {
                    _females = parentComboBoxMother.ParentList = value[0];
                    _males = parentComboBoxFather.ParentList = value[1];
                }
            }
        }
        public List<int>[] ParentsSimilarities
        {
            set
            {
                if (value != null)
                {
                    parentComboBoxMother.parentsSimilarity = value[0];
                    parentComboBoxFather.parentsSimilarity = value[1];
                }
            }
        }

        public bool ButtonEnabled { set { buttonAdd2Library.Enabled = value; } }

        public bool ShowSaveButton
        {
            set
            {
                buttonSaveChanges.Visible = value;
                buttonAdd2Library.Location = new Point((value ? 154 : 88), 362);
                buttonAdd2Library.Size = new Size((value ? 68 : 134), 37);
            }
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {
            if (!parentListValid)
                ParentListRequested?.Invoke(this);
        }

        private void dhmsInputGrown_TextChanged(object sender, EventArgs e)
        {
            if (updateMaturation && speciesIndex >= 0 && Values.V.species != null && Values.V.species[speciesIndex] != null)
            {
                updateMaturation = false;
                numericUpDownWeight.Value = Values.V.species[speciesIndex].breeding != null && Values.V.species[speciesIndex].breeding.maturationTimeAdjusted > 0 ?
                    (decimal)(weightStat.Input * dhmsInputGrown.Timespan.TotalSeconds / Values.V.species[speciesIndex].breeding.maturationTimeAdjusted) : 0; // todo. remove? baby-weight is no more shown?
                updateMaturationPercentage();
                updateMaturation = true;
            }
        }

        private void numericUpDownWeight_ValueChanged(object sender, EventArgs e)
        {
            if (updateMaturation)
            {
                updateMaturation = false;
                if (Values.V.species[speciesIndex].breeding != null && weightStat.Input > 0)
                {
                    dhmsInputGrown.Timespan = new TimeSpan(0, 0, (int)(Values.V.species[speciesIndex].breeding.maturationTimeAdjusted * (1 - (double)numericUpDownWeight.Value / weightStat.Input)));
                    dhmsInputGrown.changed = true;
                }
                else dhmsInputGrown.Timespan = TimeSpan.Zero;
                updateMaturationPercentage();
                updateMaturation = true;
            }
        }

        private void updateMaturationPercentage()
        {
            labelGrownPercent.Text = dhmsInputGrown.Timespan.TotalMinutes > 0 && weightStat.Input > 0 ?
                Math.Round(100 * (double)numericUpDownWeight.Value / weightStat.Input, 1) + " %" : "";
        }

        public DateTime Cooldown
        {
            set { dhmsInputCooldown.Timespan = value - DateTime.Now; }
            get { return dhmsInputCooldown.changed ? DateTime.Now.Add(dhmsInputCooldown.Timespan) : DateTime.Now; }
        }

        public DateTime Grown
        {
            set { dhmsInputGrown.Timespan = value - DateTime.Now; }
            get { return dhmsInputGrown.changed ? DateTime.Now.Add(dhmsInputGrown.Timespan) : DateTime.Now; }
        }

        public string[] AutocompleteOwnerList
        {
            set
            {
                var l = new AutoCompleteStringCollection();
                l.AddRange(value);
                textBoxOwner.AutoCompleteCustomSource = l;
            }
        }

        public string[] AutocompleteTribeList
        {
            set
            {
                var l = new AutoCompleteStringCollection();
                l.AddRange(value);
                textBoxTribe.AutoCompleteCustomSource = l;
            }
        }

        public string[] OwnersTribes
        {
            set
            {
                _ownersTribes = value;
            }
        }

        public string[] ServersList
        {
            set
            {
                var l = new AutoCompleteStringCollection();
                l.AddRange(value);
                cbServer.AutoCompleteCustomSource = l;
            }
        }

        public DateTime domesticatedAt
        {
            set
            {
                if (value < dateTimePickerAdded.MinDate)
                    dateTimePickerAdded.Value = dateTimePickerAdded.MinDate;
                else
                    dateTimePickerAdded.Value = value;
            }
            get { return dateTimePickerAdded.Value; }
        }

        public bool Neutered
        {
            set { checkBoxNeutered.Checked = value; }
            get { return checkBoxNeutered.Checked; }
        }

        public int MutationCounter
        {
            set
            {
                int v = value;
                if (v > numericUpDownMutations.Maximum) v = (int)numericUpDownMutations.Maximum;
                numericUpDownMutations.Value = v;
                mutationManuallyChanged = false;
            }
            get { return (int)numericUpDownMutations.Value; }
        }

        public double babyWeight
        {
            set
            {
                if (value <= (double)numericUpDownWeight.Maximum)
                    numericUpDownWeight.Value = (decimal)value;
            }
        }

        public int SpeciesIndex
        {
            set
            {
                speciesIndex = value;
                bool breedingPossible = Values.V.species.Count > value && Values.V.species[speciesIndex].breeding != null;

                dhmsInputCooldown.Visible = breedingPossible;
                dhmsInputGrown.Visible = breedingPossible;
                numericUpDownWeight.Visible = breedingPossible;
                label4.Visible = breedingPossible;
                label5.Visible = breedingPossible;
                label6.Visible = breedingPossible;
                labelGrownPercent.Visible = breedingPossible;
                numericUpDownMutations.Visible = breedingPossible;
                labelMutations.Visible = breedingPossible;
                if (!breedingPossible)
                {
                    numericUpDownWeight.Value = 0;
                    dhmsInputGrown.Timespan = TimeSpan.Zero;
                    dhmsInputCooldown.Timespan = TimeSpan.Zero;
                }
            }
        }

        private void parentComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateMutations();
        }

        private void updateMutations()
        {
            if (!mutationManuallyChanged)
            {
                numericUpDownMutations.Value = (parentComboBoxMother.SelectedParent != null ? parentComboBoxMother.SelectedParent.mutationCounter : 0) +
                    (parentComboBoxFather.SelectedParent != null ? parentComboBoxFather.SelectedParent.mutationCounter : 0);
                mutationManuallyChanged = false;
            }
        }

        private void numericUpDownMutations_ValueChanged(object sender, EventArgs e)
        {
            mutationManuallyChanged = true;
        }

        private void btnGenerateUniqueName_Click(object sender, EventArgs e)
        {
            MouseEventArgs me = (MouseEventArgs)e;
            if (me.Button == MouseButtons.Left)
            {
                if (speciesIndex >= 0 && speciesIndex < Values.V.species.Count)
                {
                    CreatureDataRequested?.Invoke(this, false);
                }
            }
        }

        private void btnGenerateUniqueName_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                CreatureDataRequested?.Invoke(this, true); // TODO doesn't get called when right-clicking. Why?
            }
        }

        /// <summary>
        /// Generates a creature name with a given pattern
        /// </summary>
        public void generateCreatureName(Creature creature)
        {
            setCreatureData(creature);
            CreatureName = uiControls.NamePatterns.generateCreatureName(creature, _females, _males);
        }

        public void openNamePatternEditor(Creature creature)
        {
            setCreatureData(creature);
            var pe = new uiControls.PatternEditor(creature, _females, _males);
            if (pe.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.sequentialUniqueNamePattern = pe.NamePattern;
            }
        }

        private void setCreatureData(Creature cr)
        {
            cr.Mother = mother;
            cr.Father = father;
            cr.species = Values.V.species[speciesIndex].name;
            cr.gender = sex;
            cr.mutationCounter = (int)numericUpDownMutations.Value;
        }

        private void textBoxOwner_Leave(object sender, EventArgs e)
        {
            // if tribe is not yet given and player has a given tribe, set tribe
            if (textBoxTribe.Text.Length == 0)
            {
                int i = textBoxOwner.AutoCompleteCustomSource.IndexOf(textBoxOwner.Text);
                if (i >= 0 && i < _ownersTribes.Length)
                {
                    textBoxTribe.Text = _ownersTribes[i];
                }
            }
        }

        private void lblOwner_Click(object sender, EventArgs e)
        {
            ownerLock = !ownerLock;
            textBoxOwner.BackColor = ownerLock ? Color.LightGray : SystemColors.Window;
        }

        private void lblName_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textBoxName.Text);
        }

        private void lblTribe_Click(object sender, EventArgs e)
        {
            tribeLock = !tribeLock;
            textBoxTribe.BackColor = tribeLock ? Color.LightGray : SystemColors.Window;
        }
    }
}
