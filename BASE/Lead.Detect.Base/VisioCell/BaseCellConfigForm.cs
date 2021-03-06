﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using WeifenLuo.WinFormsUI.Docking;
using Lead.Detect.Interfaces;

namespace Lead.Detect.DefaultCell
{
    public partial class BaseCellConfigForm : DockContent
    {
        private ICell _cell = null;
        private BaseCellConfig _cellConfig = null;
        public BaseCellConfigForm()
        {
            InitializeComponent();
        }
        public BaseCellConfigForm(ICell cell, BaseCellConfig cellConfig)
        {
            InitializeComponent();
            _cell = cell;
            _cellConfig = cellConfig;

            if(cellConfig.PrimConfigList.Count < 0) { return; }

            foreach(SingleCellPrimConfig item in cellConfig.PrimConfigList)
            {
                int index = this.dtgvPrim.Rows.Add();
                this.dtgvPrim.Rows[index].Cells[0].Value = item.PrimName;
                this.dtgvPrim.Rows[index].Cells[1].Value = item.PrimGuid;
                this.dtgvPrim.Rows[index].Cells[2].Value = item.OutputUIDis;
                this.dtgvPrim.Rows[index].Cells[3].Value = item.ConfigUIDis;
                this.dtgvPrim.Rows[index].Cells[4].Value = item.DebugUIDis;

                if (item.TabType == TabDisMode.Default)
                {
                    this.dtgvPrim.Rows[index].Cells[5].Value = "";
                }
                else if (item.TabType == TabDisMode.SingleTab)
                {
                    this.dtgvPrim.Rows[index].Cells[5].Value = "SingleTab";
                }
                else if (item.TabType == TabDisMode.MultiTab)
                {
                    this.dtgvPrim.Rows[index].Cells[5].Value = "MultiTab";
                }

                this.dtgvPrim.Rows[index].Cells[6].Value = item.TabIndex;
                this.dtgvPrim.Rows[index].Cells[7].Value = item.ControlIndex;
            }

            //SingleCellPrimConfig model = new SingleCellPrimConfig();
            //foreach (var p in model.GetType().GetProperties())
            //{
            //    _primDtSrc.Columns.Add(p.Name, p.GetType());
            //}

            //for (int i = 0; i < dr.Table.Columns.Count; i++)
            //{
            //PropertyInfo propertyInfo = model.GetType().GetProperty(dr.Table.Columns[i].ColumnName);
            //if (propertyInfo != null && dr[i] != DBNull.Value)
            //propertyInfo.SetValue(model, dr[i], null);
            //}

            //dtgvPrim.DataSource = _primDtSrc;

        }

        private void dtgvPrim_DragDrop(object sender, DragEventArgs e)
        {
            TreeNode myNode = null;
            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                myNode = (TreeNode)(e.Data.GetData(typeof(TreeNode)));
            }
            else
            {
                MessageBox.Show("error");
            }

            Guid primGuid = (Guid)myNode.Tag;
            string primName = myNode.Text;

            foreach(DataGridViewRow row in dtgvPrim.Rows)
            {
                if(row.Cells[1].Value == null) { continue; }

                if ((row.Cells[1].Value.ToString()) == (primGuid.ToString()))
                {
                    return;
                }
            }

            //DataGridViewRow rowT = new DataGridViewRow();
            //rowT.Cells[0].Value = primName;
            //rowT.Cells[1].Value = primGuid.ToString();

            dtgvPrim.Rows.Add(primName, primGuid);
        }

        private void dtgvPrim_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TreeNode)))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void tStripLbApplicate_Click(object sender, EventArgs e)
        {
            if(_cell == null) { return; }

            _cellConfig.PrimConfigList.Clear();

            foreach (DataGridViewRow row in dtgvPrim.Rows)
            {
                if (row.Cells[0].Value == null) { continue; }
                if (row.Cells[1].Value == null) { continue; }

                SingleCellPrimConfig config = new SingleCellPrimConfig();
                config.PrimName = (string)row.Cells[0].Value;
                config.PrimGuid = (Guid)row.Cells[1].Value;
                config.OutputUIDis = (Boolean)row.Cells[2].Value;
                config.ConfigUIDis = (Boolean)row.Cells[3].Value;
                config.DebugUIDis = (Boolean)row.Cells[4].Value;
                if(row.Cells[5].Value == null)
                {
                    config.TabType = TabDisMode.Default;
                }else if(row.Cells[5].Value.ToString() == "SingleTab")
                {
                    config.TabType = TabDisMode.SingleTab;
                }
                else if (row.Cells[5].Value.ToString() == "MultiTab")
                {
                    config.TabType = TabDisMode.MultiTab;
                }

                int tabIndex = -1;
                int controlIndex = -1;
                if(row.Cells[6].Value != null)
                {
                    int.TryParse(row.Cells[6].Value.ToString(), out tabIndex);
                }
                if (row.Cells[7].Value != null)
                {
                    int.TryParse(row.Cells[7].Value.ToString(), out controlIndex);
                }

                config.TabIndex = tabIndex;
                config.ControlIndex = controlIndex;

                _cellConfig.PrimConfigList.Add(config);
            }
        }

        private void tStripLbUpdate_Click(object sender, EventArgs e)
        {
            ((BaseCell)_cell).CellUpdateOutputUI();
        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}
