
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
Imports System.Text.RegularExpressions
Imports System.Xml
Imports SpectrePatcherLib

Module Module1

    Public Function DownloadFile(destPath As String, downloadLink As String) As String
        Dim downloadLinkParts As String() = downloadLink.Split("/"c)
        Dim destFile As String = IO.Path.Combine(destPath, downloadLinkParts(downloadLinkParts.Length - 1))
        If IO.File.Exists(destFile) Then
            Console.WriteLine("**Datei bereits heruntergeladen")
            Return Nothing
        Else

            Using client As New Net.WebClient
                client.DownloadFile(downloadLink, destFile)
            End Using
            Console.WriteLine("**Datei heruntergeladen")
            Return destFile
        End If
    End Function






    Sub Main()



        Dim plattformType = "windows 10"
        If OS.IsWindowsServer() Then
            plattformType = "windows server"
        End If

        Dim architecture = "x64"
        If Not System.Environment.Is64BitOperatingSystem Then
            architecture = "x86"
        End If
        Dim downloadDir = System.Configuration.ConfigurationManager.AppSettings("downloadDir")
        Dim logFile = System.Configuration.ConfigurationManager.AppSettings("logFile")

        Dim doReboot As Boolean = False
        Try
            If Not IO.Directory.Exists(downloadDir) Then
                IO.Directory.CreateDirectory(downloadDir)
            End If
        Catch ex As Exception
        End Try
        Dim versionFile As String = IO.Path.Combine(downloadDir, My.Application.Info.Version.ToString)
        If Not IO.File.Exists(versionFile) Then
            For Each file As String In IO.Directory.GetFiles(downloadDir)
                IO.File.Delete(file)
            Next
            IO.File.Create(versionFile)
        End If

        Dim helper As New SpectrePatcherHelper

        If helper.IsAMD Then
            SpectrePatcherHelper.StartProcess("cmd.exe", "/c pectreRegistryPatchAMD.cmd")
        Else
            SpectrePatcherHelper.StartProcess("cmd.exe", "/c SpectreRegistryPatchIntel.cmd")
        End If


        Dim availableUpdates As List(Of String) = helper.GetUpdateList

        For Each update As String In availableUpdates
            Try


                Console.WriteLine("******Prüfe Update: " & update & "******")
                Console.WriteLine()


                Dim availableDownloads As Dictionary(Of String, String) = helper.SearchDownloadsForUpdate(update)
                For Each download As KeyValuePair(Of String, String) In availableDownloads
                    Try
                        Dim linkText As String = download.Value.ToLowerInvariant

                        If linkText.Contains(plattformType) AndAlso linkText.Contains(architecture) Then
                            Console.WriteLine("*Treffer gefunden: " & download.Value)
                            Console.WriteLine("**GUID:" & download.Key)
                            Console.WriteLine("**Rufe Download-Link ab")
                            Dim downloadLink As String = helper.GetDownloadLink(download.Key)
                            If Not String.IsNullOrEmpty(downloadLink) Then
                                Console.WriteLine("***Download-Link:" & downloadLink)



                                Console.WriteLine("**Lade herunter")
                                Dim destFile As String = DownloadFile(downloadDir, downloadLink)


                                If Not String.IsNullOrEmpty(destFile) Then
                                    Try
                                        Console.WriteLine("**Installiere")


                                        Dim exitCode As Integer = SpectrePatcherHelper.StartProcess("wusa.exe", destFile & " /quiet /norestart")
                                        If exitCode <> 0 Then
                                            If exitCode = -2145124329 Then
                                                'Update trifft auf System nicht zu.
                                            ElseIf ExitCode = 2359302 Then
                                                'Update bereits installiert.
                                            ElseIf ExitCode = 3010 Then
                                                'Neustart erforderlich#
                                                doReboot = True
                                            Else
                                                Throw New Exception(String.Format("Installation mit {0} fehlgeschlagen", exitCode))

                                            End If
                                        End If

                                        Console.WriteLine("**Installiert")

                                    Catch ex As Exception
                                        IO.File.Delete(destFile)
                                        Throw ex
                                    End Try
                                End If

                            End If


                            Console.WriteLine()
                        End If
                    Catch ex As Exception

                        helper.LogError(ex)
                    End Try
                Next
            Catch ex As Exception
                helper.LogError(ex)
            End Try

        Next


        Console.WriteLine("Fertig")
        If helper.ErrorLog.Count > 0 Then
            Console.WriteLine("Fehler: ")
            For Each errorStr As String In helper.ErrorLog
                Console.WriteLine(errorStr)
            Next

        End If
        System.IO.File.AppendAllLines(logFile, helper.ErrorLog)

        If doReboot AndAlso System.Configuration.ConfigurationManager.AppSettings("reboot").ToLowerInvariant.Equals("true") Then

            Reboot()
        End If
    End Sub



    Private Sub Reboot()
        SpectrePatcherHelper.StartProcess("cmd", "/C shutdown -f -r -t 5")
    End Sub

End Module
