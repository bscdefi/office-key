﻿/*
 *      [LKY Common Tools] Copyright (C) 2022 liukaiyuan@sjtu.edu.cn Inc.
 *      
 *      FileName : Lib_OfficeDownload.cs
 *      Developer: liukaiyuan@sjtu.edu.cn (Odysseus.Yuan)
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using static LKY_OfficeTools.Lib.Lib_AppInfo;
using static LKY_OfficeTools.Lib.Lib_AppLog;
using static LKY_OfficeTools.Lib.Lib_OfficeInfo;
using static LKY_OfficeTools.Lib.Lib_OfficeInfo.OfficeLocalInstall;

namespace LKY_OfficeTools.Lib
{
    /// <summary>
    /// Office 下载类库
    /// </summary>
    internal class Lib_OfficeDownload
    {
        /// <summary>
        /// 下载文件列表
        /// </summary>
        static List<string> down_list = null;

        /// <summary>
        /// 重载实现下载
        /// </summary>
        internal Lib_OfficeDownload()
        {
            //FilesDownload();
        }

        /// <summary>
        /// 下载所有文件（Aria2c）
        /// 返回值：-1【用户终止】，0【下载失败】，1【下载成功】，2【无需下载】
        /// </summary>
        internal static int FilesDownload()
        {
            try
            {
                //获取下载列表
                down_list = OfficeNetVersion.GetOfficeFileList();
                if (down_list == null)
                {
                    //因列表获取异常，停止下载
                    return 0;
                }

                //判断是否已经安装了当前版本
                InstallState install_state = GetOfficeState();
                if (install_state==InstallState.Correct)                //已安装最新版，无需下载
                {
                    new Log($"\n      * 当前系统安装了最新 Office 版本，已跳过下载、安装流程。", ConsoleColor.DarkMagenta);
                    return 2;
                }
                ///当不存在 VersionToReport or 其版本与最新版不一致 or 产品ID不一致 or 安装位数与系统不一致时，需要下载新文件。

                //定义下载目标地
                string save_to = AppPath.ExecuteDir + @"\Office\Data\";       //文件必须位于 \Office\Data\ 下，
                                                                           //ODT安装必须在 Office 上一级目录上执行。

                //计划保存的地址
                List<string> save_files = new List<string>();

                //下载开始
                new Log($"\n------> 开始下载 Office v{OfficeNetVersion.latest_version} 文件 ...", ConsoleColor.DarkCyan);
                //延迟，让用户看到开始下载
                Thread.Sleep(1000);

                //轮询下载所有文件
                foreach (var a in down_list)
                {
                    //根据官方目录，来调整下载保存位置
                    string save_path = save_to + a.Substring(OfficeNetVersion.office_file_root_url.Length).Replace("/", "\\");

                    //保存到List里面，用于后续检查
                    save_files.Add(save_path);

                    new Log($"\n     >> 下载 {new FileInfo(save_path).Name} 文件中，请稍候 ...", ConsoleColor.DarkYellow);

                    //遇到重复的文件可以断点续传
                    int down_result = Lib_Aria2c.DownFile(a, save_path);
                    if (down_result != 1)
                    {
                        //如果因为核心下载exe丢失，导致下载失败，直接中止
                        throw new Exception($"下载 {a} 异常！");
                    }

                    //如果用户中断了下载，则直接跳出
                    if (Lib_AppState.Current_StageType == Lib_AppState.ProcessStage.Interrupt)
                    {
                        return -1;
                    }

                    new Log($"     √ 已下载 {new FileInfo(save_path).Name} 文件。", ConsoleColor.DarkGreen);
                }

                new Log($"\n------> 正在检查 Office v{OfficeNetVersion.latest_version} 文件 ...", ConsoleColor.DarkCyan);

                foreach (var b in save_files)
                {
                    string aria_tmp_file = b + ".aria2c";

                    //下载完成的标志：文件存在，且不存在临时文件
                    if (File.Exists(b) && !File.Exists(aria_tmp_file))
                    {
                        new Log($"     >> 检查 {new FileInfo(b).Name} 文件中 ...", ConsoleColor.DarkYellow);
                    }
                    else
                    {
                        new Log($"     >> 文件 {new FileInfo(b).Name} 存在异常，重试中 ...", ConsoleColor.DarkRed);
                        return FilesDownload();
                    }
                }

                new Log($"     √ 已完成 Office v{OfficeNetVersion.latest_version} 下载。", ConsoleColor.DarkGreen);

                return 1;
            }
            catch (Exception Ex)
            {
                new Log(Ex.ToString());
                return 0;
            }
        }
    }
}
