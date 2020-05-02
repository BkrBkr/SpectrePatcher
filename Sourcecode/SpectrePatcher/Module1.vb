﻿
'Copyright(C) 2019 Björn Kremer

'This file Is part Of SpectrePatcher.

'SpectrePatcher Is free software: you can redistribute it And/Or modify
'it under the terms Of the GNU General Public License As published by
'the Free Software Foundation, either version 3 Of the License, Or
'(at your option) any later version.

'SpectrePatcher Is distributed In the hope that it will be useful,
'but WITHOUT ANY WARRANTY; without even the implied warranty of
'MERCHANTABILITY Or FITNESS FOR A PARTICULAR PURPOSE.  See the
'GNU General Public License For more details.

'You should have received a copy Of the GNU General Public License
'along with SpectrePatcher. If Not, see < http: //www.gnu.org/licenses/>.

Imports System.IO
Imports System.Net
Imports System.Reflection
Imports System.Text.RegularExpressions
Imports System.Xml
Imports SpectrePatcherLib

Module Module1

    Private Sub autoUpdate(logFile As String)
        Dim exePath As String = System.Reflection.Assembly.GetEntryAssembly().Location
        Dim workingDir As String = New System.IO.FileInfo(exePath).Directory.FullName
        Dim version As Version = Assembly.GetExecutingAssembly().GetName().Version

        Dim updateExe As String = IO.Path.Combine(workingDir, "SimpleAutoUpdate.NET.exe")

        CopyUpdate(workingDir)

        Dim fileName = "cmd.exe"
        Dim arguments = String.Format("/C """"{0}"" -currentVersion=""{1}"" -updateManifestUrl=""{2}"" -downloadServerPrefix=""{3}"" -pathToMainProgram=""{4}"" 2>> ""{5}""""", updateExe, version.ToString(), "https://raw.githubusercontent.com/BkrBkr/SpectrePatcher/master/update.xml", "https://github.com/BkrBkr/", exePath, logFile)
        Dim exitCode As Integer = SpectrePatcherHelper.StartProcess(fileName, arguments)

        CopyUpdate(workingDir)

        If exitCode <> 0 Then
            Throw New InvalidOperationException(String.Format("Error during auto update. Error-Code {0}", exitCode))
        End If
    End Sub

    Private Sub CopyUpdate(workingDir As String)
        Dim d As New DirectoryInfo(workingDir)
        For Each file As FileInfo In d.GetFiles("*.update")
            Dim destFile As String = file.Name.Substring(0, file.Name.Length - ".update".Length)
            IO.File.Delete(destFile)
            file.MoveTo(destFile)
        Next
    End Sub


    Sub Main()
        Dim logFile = System.Configuration.ConfigurationManager.AppSettings("logFile")
        Dim downloadDir = System.Configuration.ConfigurationManager.AppSettings("downloadDir")

        Dim helper As New SpectrePatcherHelper
        Try
            autoUpdate(logFile)

            helper.RunLogic(logFile, downloadDir)

        Catch ex As Exception
            helper.LogError(ex)
        Finally
            helper.PrintErrors(logFile)
        End Try
        Environment.Exit(Environment.ExitCode)

    End Sub




End Module
