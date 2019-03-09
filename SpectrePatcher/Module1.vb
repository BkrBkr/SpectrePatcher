'Copyright(C) 2019 Björn Kremer

'This file Is part Of SpectrePatcher.

'SpectrePatcher Is free software: you can redistribute it And/Or modify
'it under the terms Of the GNU General Public License As published by
'the Free Software Foundation, either version 3 Of the License, Or
'(at your option) any later version.

'Foobar Is distributed In the hope that it will be useful,
'but WITHOUT ANY WARRANTY; without even the implied warranty of
'MERCHANTABILITY Or FITNESS FOR A PARTICULAR PURPOSE.  See the
'GNU General Public License For more details.

'You should have received a copy Of the GNU General Public License
'along with Foobar. If Not, see < http: //www.gnu.org/licenses/>.

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

        Dim helper As New SpectrePatcherHelper

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
                                    Console.WriteLine("**Installiere")

                                    Using p As New Process

                                        p.StartInfo.FileName = destFile
                                        p.StartInfo.Arguments = "/quiet /norestart"
                                        p.StartInfo.CreateNoWindow = True
                                        p.StartInfo.UseShellExecute = True
                                        p.Start()
                                        Dim start As DateTime = DateTime.Now.AddSeconds(60)
                                        While Not p.HasExited
                                            If start < DateTime.Now Then
                                                Throw New TimeoutException
                                            End If
                                            System.Threading.Thread.Sleep(1000)
                                        End While


                                    End Using
                                    Console.WriteLine("**Installiert")
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
        System.IO.File.WriteAllLines(logFile, helper.ErrorLog)


    End Sub

End Module
