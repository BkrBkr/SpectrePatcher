
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
        Try
            If IO.File.Exists(updateExe) Then
                IO.File.Delete(updateExe)
            End If
        Catch ex As Exception

        End Try

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
