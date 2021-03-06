﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Lead.Detect.FrameworkExtension;
using Lead.Detect.FrameworkExtension.elementExtensionInterfaces;
using Lead.Detect.FrameworkExtension.platforms.calibrations;
using Lead.Detect.FrameworkExtension.platforms.motionPlatforms;
using Lead.Detect.FrameworkExtension.stateMachine;
using Lead.Detect.ThermoAOI.Machine1.Common;
using Lead.Detect.ThermoAOI.Machine1.UserDefine;
using Lead.Detect.ThermoAOI.Machine1.UserDefine.newTasks;
using Lead.Detect.Utility.FittingHelper;

namespace Lead.Detect.ThermoAOI.Machine1.Calibration
{
    /// <summary>
    /// gt/gt1/gt2 高度坐标系标定
    /// </summary>
    class AutoPlatformHeightCalib : AutoCalib
    {
        #region trans

        [Category("传送上料配置")]
        public ICylinderEx do_clampy_cy { get; set; }
        [Category("传送上料配置")]
        public PlatformEx PlatformCarrier { get; set; }

        #endregion

        #region up platform

        [Category("上工站配置")]
        public PlatformEx Platform1 { get; set; }
        [Category("上工站配置")]
        public PosXYZ Platform1GTCalibPos { get; set; }
        [Category("上工站配置")]
        public double JumpHeight1 { get; set; } = -50;
        public List<PosXYZ> Platform1GTPlaneCalibPos { get; set; }

        #endregion

        #region down platform

        [Category("下工站配置")]
        public PlatformEx Platform2 { get; set; }

        [Category("下工站配置")]
        public PosXYZ Platform2GT1CalibPos { get; set; }
        [Category("下工站配置")]
        public PosXYZ Platform2GT2CalibPos { get; set; }

        [Category("下工站配置")]
        public double JumpHeight2 { get; set; } = -15;
        [Category("下工站配置")]
        public ICylinderEx do_gt2_cy { get; set; }
        public List<PosXYZ> Platform2GT1PlaneCalibPos { get; set; }
        public List<PosXYZ> Platform2GT2PlaneCalibPos { get; set; }

        #endregion

        #region misc

        public KeyenceGT GtController { get; set; }
        public PosXYZ StandardHeight { get; set; }

        #endregion


        [Category("OUTPUT")]
        public PosXYZ OutputGTCalibPos { get; set; }
        [Category("OUTPUT")]
        public PlaneParams OutputUpStandardPlane { get; set; }


        [Category("OUTPUT")]
        public PosXYZ OutputGT1CalibPos { get; set; }
        [Category("OUTPUT")]
        public PosXYZ OutputGT2CalibPos { get; set; }
        [Category("OUTPUT")]
        public PlaneParams OutputDownStandardPlane { get; set; }
        [Category("OUTPUT")]
        public PlaneParams OutputDownStandardPlane2 { get; set; }
        [Category("OUTPUT")]
        public PosXYZ OutputStandardHeight { get; set; }



        public override void InitCalib()
        {
            do_gt2_cy?.SetDo(this, false, ignoreOrWaringOrError: null);
            do_clampy_cy.SetDo(this, ignoreOrWaringOrError: null);

            PlatformCarrier?.EnterAuto(this).MoveAbs("Work");
        }

        public override void UninitCalib()
        {
            do_gt2_cy?.SetDo(this, false, ignoreOrWaringOrError: null);

            if (Platform1 != null)
            {
                Platform1.EnterAuto(this).Home(2);
                Platform1.EnterAuto(this).MoveAbs("Wait", checkLimit: false);

                Platform1.ExitAuto();
            }


            if (Platform2 != null)
            {
                Platform2.EnterAuto(this).Home(2);
                Platform2.EnterAuto(this).MoveAbs("Wait", checkLimit: false);

                Platform2.ExitAuto();
            }


            PlatformCarrier?.EnterAuto(this).MoveAbs("Wait");

            do_clampy_cy.SetDo(this, false);

            PlatformCarrier?.ExitAuto();
        }


        public override void DoCalib()
        {
            OutputStandardHeight = StandardHeight;

            Log("上平台GT高度标定\n----------------------------------------------------------");
            {
                //GT标定高度
                OutputGTCalibPos = Platform1GTCalibPos;
                DataList.Add(OutputGTCalibPos.ToString());

                //标准平面标定
                if (Platform1GTPlaneCalibPos != null)
                {
                    //update z
                    if (Platform1 != null && GtController != null)
                    {
                        bool isFirst = true;

                        foreach (var calibPos in Platform1GTPlaneCalibPos)
                        {   
                            //product to up platform
                            var pos = new PosXYZ(calibPos.Data()) { Z = Platform1GTCalibPos.Z };
                            if (isFirst)
                            {
                                isFirst = false;
                                Platform1?.EnterAuto(this).Jump(Platform1.GetPos("P->UP", pos.Data()), 0);
                            }
                            else
                            {
                                Platform1?.EnterAuto(this).Jump(Platform1.GetPos("P->UP", pos.Data()), JumpHeight1);
                            }
                            Thread.Sleep(1000);
                            calibPos.Z = GtController.ReadData()[0];
                            DataList.Add(calibPos.ToString());
                        }
                    }

                    var fitplane = PlaneParams.FitPlane(Platform1GTPlaneCalibPos);
                    OutputUpStandardPlane = new PlaneParams() { Normal = fitplane.Normal, Origin = fitplane.Origin };
                    DataList.Add(OutputUpStandardPlane.ToString());
                }
            }

            //复位上平台
            Platform1?.EnterAuto(this).Jump("Wait", JumpHeight1);
            Log("上平台GT高度标定 完成\n----------------------------------------------------------");


            //下GT高度标定

            //复位下平台Z轴
            if (Platform2 != null)
            {
                Platform2.EnterAuto(this).Home(2);
                Platform2.EnterAuto(this).MoveAbs(2, "Wait", checkLimit: false);
            }

            Log("下平台GT1高度标定\n----------------------------------------------------------");
            {
                do_gt2_cy?.SetDo(this, true, ignoreOrWaringOrError: null);
                {

                    //GT1GT2 高度差标定
                    OutputGT1CalibPos = Platform2GT1CalibPos;
                    //update z
                    if (Platform2 != null && GtController != null)
                    {
                        Platform2.EnterAuto(this).Jump(Platform2GT1CalibPos, 0);
                        Thread.Sleep(1000);
                        OutputGT1CalibPos.OffsetZ = GtController.ReadData()[1];
                    }

                    //GT1GT2 高度差标定
                    OutputGT2CalibPos = Platform2GT2CalibPos;
                    //update z
                    if (Platform2 != null && GtController != null)
                    {
                        Platform2.EnterAuto(this).Jump(Platform2GT2CalibPos, JumpHeight2);
                        Thread.Sleep(1000);
                        OutputGT2CalibPos.OffsetZ = GtController.ReadData()[2];
                    }


                    //GT1标准平面标定
                    if (Platform2GT1PlaneCalibPos != null)
                    {
                        //update z
                        if (Platform2 != null && GtController != null)
                        {
                            foreach (var calibPos in Platform2GT1PlaneCalibPos)
                            {
                                //product to up platform
                                var pos = new PosXYZ(calibPos.Data()) { Z = Platform2GT1CalibPos.Z };
                                Platform2.EnterAuto(this).Jump(Platform2.GetPos("P->DOWN1", pos.Data()), JumpHeight2);
                                Thread.Sleep(1000);
                                calibPos.Z = GtController.ReadData()[1];
                                DataList.Add(calibPos.ToString());
                            }
                        }

                        var fitplane = PlaneParams.FitPlane(Platform2GT1PlaneCalibPos);
                        OutputDownStandardPlane = new PlaneParams() { Normal = fitplane.Normal, Origin = fitplane.Origin };
                        DataList.Add(OutputDownStandardPlane.ToString());
                    }

                    //GT2标准平面标定
                    if (Platform2GT2PlaneCalibPos != null)
                    {
                        //update z
                        if (Platform2 != null && GtController != null)
                        {
                            foreach (var calibPos in Platform2GT2PlaneCalibPos)
                            {
                                //product to up platform
                                var pos = new PosXYZ(calibPos.Data()) { Z = Platform2GT2CalibPos.Z };
                                Platform2.EnterAuto(this).Jump(Platform2.GetPos("P->DOWN2", pos.Data()), JumpHeight2);
                                Thread.Sleep(1000);
                                calibPos.Z = GtController.ReadData()[2];
                                DataList.Add(calibPos.ToString());
                            }
                        }

                        var fitplane = PlaneParams.FitPlane(Platform2GT2PlaneCalibPos);
                        OutputDownStandardPlane2 = new PlaneParams() { Normal = fitplane.Normal, Origin = fitplane.Origin };
                        DataList.Add(OutputDownStandardPlane2.ToString());
                    }

                }
                do_gt2_cy?.SetDo(this, false, ignoreOrWaringOrError: null);
            }


            {
                //复位下平台
                //move platform2 wait
                if (Platform2 != null)
                {
                    Platform2.EnterAuto(this).Home(2);
                    Platform2.EnterAuto(this).MoveAbs("Wait", checkLimit: false);
                }
                Log("下平台GT1/2高度标定  完成\n----------------------------------------------------------");
            }

            //output
            DataList.Add(OutputGT1CalibPos.ToString());
            DataList.Add(OutputGT2CalibPos.ToString());
            DataList.Add(OutputStandardHeight.ToString());

            DataList.Add(OutputUpStandardPlane.ToString());
            DataList.Add(OutputDownStandardPlane.ToString());
        }




        public static AutoPlatformHeightCalib CreateHeightCalib(string platform)
        {
            switch (platform)
            {
                case "Left":
                    return new AutoPlatformHeightCalib
                    {
                        CalibInfo = $"LeftPlatformHeight",
                        Description = "Left",
                        Station = Machine.Ins.Find<Station>("LeftStation"),

                        //carrier
                        PlatformCarrier = Machine.Ins.Find<PlatformEx>("LeftCarrier"),

                        do_clampy_cy = Machine.Ins.Find<ICylinderEx>("LClampCylinderY"),

                        //up
                        Platform1 = Machine.Ins.Find<PlatformEx>("LeftUp"),
                        Platform1GTCalibPos = Machine.Ins.Find<PlatformEx>("LeftUp")["HeightCalib"] as PosXYZ,
                        Platform1GTPlaneCalibPos = Machine.Ins.Find<PlatformEx>("LeftUp").Positions.FindAll(p => p.Name.StartsWith("HeightAlign")).Cast<PosXYZ>().ToList(),

                        //down
                        Platform2 = Machine.Ins.Find<PlatformEx>("LeftDown"),
                        Platform2GT1CalibPos = Machine.Ins.Find<PlatformEx>("LeftDown")["HeightCalib1"] as PosXYZ,
                        Platform2GT2CalibPos = Machine.Ins.Find<PlatformEx>("LeftDown")["HeightCalib2"] as PosXYZ,
                        StandardHeight = Machine.Ins.Find<PlatformEx>("LeftDown")["StandardHeight"] as PosXYZ,
                        Platform2GT1PlaneCalibPos = Machine.Ins.Find<PlatformEx>("LeftDown").Positions.FindAll(p => p.Name.StartsWith("HeightAlign")).Cast<PosXYZ>().ToList(),
                        Platform2GT2PlaneCalibPos = Machine.Ins.Find<PlatformEx>("LeftDown").Positions.FindAll(p => p.Name.StartsWith("HeightAlign")).Cast<PosXYZ>().ToList(),

                        do_gt2_cy = Machine.Ins.Find<ICylinderEx>("LGTCylinder"),

                        JumpHeight1 = Machine.Ins.Settings.Common.LJumpHeightUp,
                        JumpHeight2 = Machine.Ins.Settings.Common.LJumpHeightDown,

                        GtController = (Machine.Ins.Find<StationTask>("LeftTrans") as newTransTask)?.GtController,
                    };

                case "Right":
                    return new AutoPlatformHeightCalib
                    {
                        CalibInfo = $"RightPlatformHeight",
                        Description = "RIGHT",
                        Station = Machine.Ins.Find<Station>("RightStation"),

                        //carrier
                        PlatformCarrier = Machine.Ins.Find<PlatformEx>("RightCarrier"),
                        do_clampy_cy = Machine.Ins.Find<ICylinderEx>("RClampCylinderY"),

                        //up
                        Platform1 = Machine.Ins.Find<PlatformEx>("RightUp"),
                        Platform1GTCalibPos = Machine.Ins.Find<PlatformEx>("RightUp")["HeightCalib"] as PosXYZ,
                        Platform1GTPlaneCalibPos = Machine.Ins.Find<PlatformEx>("RightUp").Positions.FindAll(p => p.Name.StartsWith("HeightAlign")).Cast<PosXYZ>().ToList(),


                        //down
                        Platform2 = Machine.Ins.Find<PlatformEx>("RightDown"),
                        Platform2GT1CalibPos = Machine.Ins.Find<PlatformEx>("RightDown")["HeightCalib1"] as PosXYZ,
                        Platform2GT2CalibPos = Machine.Ins.Find<PlatformEx>("RightDown")["HeightCalib2"] as PosXYZ,
                        StandardHeight = Machine.Ins.Find<PlatformEx>("RightDown")["StandardHeight"] as PosXYZ,
                        Platform2GT1PlaneCalibPos = Machine.Ins.Find<PlatformEx>("RightDown").Positions.FindAll(p => p.Name.StartsWith("HeightAlign")).Cast<PosXYZ>().ToList(),
                        Platform2GT2PlaneCalibPos = Machine.Ins.Find<PlatformEx>("RightDown").Positions.FindAll(p => p.Name.StartsWith("HeightAlign")).Cast<PosXYZ>().ToList(),

                        do_gt2_cy = Machine.Ins.Find<ICylinderEx>("RGTCylinder"),

                        JumpHeight1 = Machine.Ins.Settings.Common.RJumpHeightUp,
                        JumpHeight2 = Machine.Ins.Settings.Common.RJumpHeightDown,

                        GtController = (Machine.Ins.Find<StationTask>("RightTrans") as newTransTask)?.GtController,
                    };
            }

            throw new Exception("Create Height Calib Fail");
        }


        public static void SaveHeightCalib(AutoPlatformHeightCalib calib)
        {
            if (MessageBox.Show($"保存{calib.Station.Name}标定数据？", "", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    if (calib.Station.Id == 1)
                    {
                        Machine.Ins.Settings.Calibration.LeftHeightCalibGtPos = calib.OutputGTCalibPos;
                        Machine.Ins.Settings.Calibration.LeftHeightCalibGt1Pos = calib.OutputGT1CalibPos;
                        Machine.Ins.Settings.Calibration.LeftHeightCalibGt2Pos = calib.OutputGT2CalibPos;

                        //new
                        Machine.Ins.Settings.Calibration.LeftUpStandardPlaneGT = calib.OutputUpStandardPlane;
                        Machine.Ins.Settings.Calibration.LeftDownStandardPlaneGT1 = calib.OutputDownStandardPlane;
                        Machine.Ins.Settings.Calibration.LeftDownStandardPlaneGT2 = calib.OutputDownStandardPlane2;


                        Machine.Ins.Settings.Calibration.LeftHeightStandard = calib.StandardHeight;
                    }
                    else if (calib.Station.Id == 2)
                    {
                        Machine.Ins.Settings.Calibration.RightHeightCalibGtPos = calib.OutputGTCalibPos;
                        Machine.Ins.Settings.Calibration.RightHeightCalibGt1Pos = calib.OutputGT1CalibPos;
                        Machine.Ins.Settings.Calibration.RightHeightCalibGt2Pos = calib.OutputGT2CalibPos;

                        //new
                        Machine.Ins.Settings.Calibration.RightUpStandardPlaneGT = calib.OutputUpStandardPlane;
                        Machine.Ins.Settings.Calibration.RightDownStandardPlaneGT1 = calib.OutputDownStandardPlane;
                        Machine.Ins.Settings.Calibration.RightDownStandardPlaneGT2 = calib.OutputDownStandardPlane2;

                        Machine.Ins.Settings.Calibration.RightHeightStandard = calib.StandardHeight;
                    }

                    Machine.Ins.Save();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存标定数据异常:{ex.Message}");
                }
            }
        }


    }
}