
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







    Sub Main()
        Dim logFile = System.Configuration.ConfigurationManager.AppSettings("logFile")
        Dim helper As New SpectrePatcherHelper
        Dim doReboot As Boolean = False
        Try
            Dim downloadDir = System.Configuration.ConfigurationManager.AppSettings("downloadDir")
            Try
                If Not IO.Directory.Exists(downloadDir) Then
                    IO.Directory.CreateDirectory(downloadDir)
                End If
            Catch ex As Exception
            End Try

            Dim plattformType = "windows 10"
            If OS.IsWindowsServer() Then
                plattformType = "windows server"
            End If

            Dim architecture = "x64"
            If Not System.Environment.Is64BitOperatingSystem Then
                architecture = "x86"
            End If

            Dim versionFile As String = IO.Path.Combine(downloadDir, My.Application.Info.Version.ToString)
            If Not IO.File.Exists(versionFile) Then
                For Each file As String In IO.Directory.GetFiles(downloadDir)
                    IO.File.Delete(file)
                Next
                IO.File.WriteAllText(versionFile, Environment.OSVersion.VersionString)
            Else
                Dim versionNumber As String = IO.File.ReadAllText(versionFile)
                If Not versionNumber.Equals(Environment.OSVersion.VersionString) Then
                    For Each file As String In IO.Directory.GetFiles(downloadDir)
                        IO.File.Delete(file)
                    Next
                End If
                IO.File.WriteAllText(versionFile, Environment.OSVersion.VersionString)
            End If



            If helper.IsAMD Then
                Console.WriteLine("******RegistryPatch AMD******")
                Dim result As Integer = SpectrePatcherHelper.StartProcess("cmd.exe", "/c " & IO.Path.Combine(My.Application.Info.DirectoryPath & "\", "SpectreRegistryPatchAMD.cmd"))
                If result <> 0 Then
                    helper.LogError("Registry-Patch nicht angebracht")
                End If
            Else
                Console.WriteLine("******RegistryPatch Intel******")
                Dim result As Integer = SpectrePatcherHelper.StartProcess("cmd.exe", "/c " & IO.Path.Combine(My.Application.Info.DirectoryPath & "\", "SpectreRegistryPatchIntel.cmd"))
                If result <> 0 Then
                    helper.LogError("Registry-Patch nicht angebracht")
                End If
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
                                    Dim destFile As String = helper.DownloadFile(downloadDir, downloadLink)


                                    If Not String.IsNullOrEmpty(destFile) Then
                                        Try
                                            Console.WriteLine("**Installiere")


                                            Dim exitCode As Integer = SpectrePatcherHelper.StartProcess("wusa.exe", destFile & " /quiet /norestart")
                                            If exitCode <> 0 Then
                                                If exitCode = -2145124329 Then
                                                    'Update trifft auf System nicht zu.
                                                ElseIf exitCode = 2359302 Then
                                                    'Update bereits installiert.
                                                ElseIf exitCode = 3010 Then
                                                    'Neustart erforderlich
                                                    Console.WriteLine("**Neustart erforderlich. Patchvorgang unvollständig")
                                                    doReboot = True
                                                    Exit For
                                                Else
                                                    Throw New Exception(String.Format("Installation mit {0} fehlgeschlagen", exitCode))

                                                End If
                                            End If

                                            Console.WriteLine("**Installiert")

                                        Catch ex As Exception
                                            If IO.File.Exists(destFile) Then
                                                IO.File.Delete(destFile)
                                            End If

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
        Catch ex As Exception
            helper.LogError(ex)
        End Try

        System.IO.File.AppendAllLines(logFile, helper.ErrorLog)
        Console.WriteLine("Fertig")
        If helper.ErrorLog.Count > 0 Then
            Environment.ExitCode = -1
            Console.WriteLine("Fehler: ")
            For Each errorStr As String In helper.ErrorLog
                Console.WriteLine(errorStr)
            Next
        Else
            Environment.ExitCode = 0
        End If


        If doReboot AndAlso System.Configuration.ConfigurationManager.AppSettings("reboot").ToLowerInvariant.Equals("true") Then

            Reboot()
        End If
        Environment.Exit(Environment.ExitCode)
    End Sub



    Private Sub Reboot()
        SpectrePatcherHelper.StartProcess("cmd", "/C shutdown -f -r -t 5")
    End Sub

End Module
