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
'along with SpectrePatcher. If Not, see < http://www.gnu.org/licenses/>.
Imports System.Management
Imports System.Net
Imports System.Text.RegularExpressions
Imports System.Xml

Public Class SpectrePatcherHelper
    Private ReadOnly Property ErrorLog As New List(Of String)

    Private Sub LogError(msg As String)
        _ErrorLog.Add(msg)
    End Sub
    Public Sub LogError(ex As Exception)
        _ErrorLog.Add(ex.Message)
    End Sub


    Public Sub RunLogic(logFile As String, downloadDir As String)

        Dim doReboot As Boolean = False
        Try

            InitializeDownloadFolder(downloadDir)

            Dim plattformType = "windows 10"
            If OS.IsWindowsServer() Then
                plattformType = "windows server"
            End If

            Dim architecture = "x64"
            If Not System.Environment.Is64BitOperatingSystem Then
                architecture = "x86"
            End If

            ApplyRegistryPatches()

            Dim availableUpdates As List(Of String) = GetUpdateList()

            For Each update As String In availableUpdates
                Try

                    Console.WriteLine("******Prüfe Update: " & update & "******")
                    Console.WriteLine()

                    Dim availableDownloads As Dictionary(Of String, String) = SearchDownloadsForUpdate(update)

                    For Each download As KeyValuePair(Of String, String) In availableDownloads
                        Try
                            Dim linkText As String = download.Value.ToLowerInvariant

                            If linkText.Contains(plattformType) AndAlso linkText.Contains(architecture) Then
                                Console.WriteLine("*Treffer gefunden: " & download.Value)
                                Console.WriteLine("**GUID:" & download.Key)
                                Console.WriteLine("**Rufe Download-Link ab")
                                doReboot = Not DownloadAndInstallPatch(download.Key, downloadDir)
                                If doReboot Then
                                    Exit For
                                End If
                                Console.WriteLine()
                            End If
                        Catch ex As Exception

                            LogError(ex)
                        End Try
                    Next
                    If doReboot Then
                        Exit For
                    End If
                Catch ex As Exception
                    LogError(ex)
                End Try
            Next
        Catch ex As Exception
            LogError(ex)
        End Try

        If doReboot AndAlso System.Configuration.ConfigurationManager.AppSettings("reboot").ToLowerInvariant.Equals("true") Then
            Reboot(logFile)
        End If

    End Sub



    Public Shared Function StartProcess(file As String, param As String) As Integer
        Using p As New Process

            p.StartInfo.FileName = file
            p.StartInfo.Arguments = param
            p.StartInfo.CreateNoWindow = True
            p.StartInfo.UseShellExecute = False
            p.Start()
            Threading.Thread.Sleep(500)
            Dim start As DateTime = DateTime.Now.AddSeconds(120)
            While Not p.HasExited
                If start < DateTime.Now Then
                    Throw New TimeoutException
                End If
                System.Threading.Thread.Sleep(1000)
            End While
            p.WaitForExit()
            Return p.ExitCode
        End Using

    End Function

    Private Function IsAMD() As Boolean
        Using mc As ManagementClass = New ManagementClass("Win32_Processor")
            Using moc As ManagementObjectCollection = mc.GetInstances()

                If moc.Count <> 0 Then

                    For Each mo As ManagementObject In mc.GetInstances()
                        If mo("Manufacturer").ToString().ToLowerInvariant.Contains("amd") Then
                            Return True
                        End If
                    Next
                End If
                Return False
            End Using
        End Using
    End Function

    Private Function GetUpdateList() As List(Of String)
        Dim retVal As New List(Of String)

        Dim rssDoc = New XmlDocument()
        rssDoc.Load("https://support.microsoft.com/app/content/api/content/feeds/sap/en-us/6ae59d69-36fc-8e4d-23dd-631d98bf74a9/rss")

        Dim updateList As XmlNodeList = rssDoc.SelectNodes("//rss/channel/item")
        If updateList.Count = 0 Then
            LogError("Keine Updates gefunden.")
        End If

        For Each update As XmlNode In updateList
            Dim title As XmlNode = update.SelectSingleNode("title")
            If title IsNot Nothing AndAlso Not String.IsNullOrEmpty(title.InnerText) Then
                If title.InnerText.ToLowerInvariant.Contains("intel microcode") Then
                    retVal.Add(title.InnerText)
                End If
            Else
                LogError("Update-Titel konnte nicht bestimmt werden.")
            End If

        Next
        Return retVal
    End Function


    Private Function SearchDownloadsForUpdate(update As String) As Dictionary(Of String, String)
        Dim retVal As New Dictionary(Of String, String)

        Dim kbNumberMatch As Match = Regex.Match(update, "(?<number>KB[0-9]{4,})", RegexOptions.IgnoreCase Or RegexOptions.Singleline Or RegexOptions.IgnorePatternWhitespace Or RegexOptions.ExplicitCapture)

        If kbNumberMatch IsNot Nothing AndAlso kbNumberMatch.Success AndAlso kbNumberMatch.Groups("number") IsNot Nothing AndAlso Not String.IsNullOrEmpty(kbNumberMatch.Groups("number").Value) Then

            Dim kbNumberString As String = kbNumberMatch.Groups("number").Value

            Dim searchResult As String = ""
            Using updateSearchClient As New WebClient()
                searchResult = updateSearchClient.DownloadString("https://www.catalog.update.microsoft.com/Search.aspx?q=" & System.Web.HttpUtility.UrlEncode(kbNumberString))
            End Using

            Dim downloadLinksMatches As MatchCollection = Regex.Matches(searchResult, "<a\s+(?<linkAttributes>[^>]+)>(?<linkText>.+?)<\/a>", RegexOptions.IgnoreCase Or RegexOptions.Singleline Or RegexOptions.IgnorePatternWhitespace Or RegexOptions.ExplicitCapture)
            If downloadLinksMatches.Count = 0 Then
                LogError("Es konnte zu dem Update keine Downloads bestimmt werden. F1")
            End If

            For Each downloadLinksMatch As Match In downloadLinksMatches
                If downloadLinksMatch.Success AndAlso downloadLinksMatch.Groups("linkAttributes") IsNot Nothing AndAlso downloadLinksMatch.Groups("linkText") IsNot Nothing Then

                    Dim linkText As String = downloadLinksMatch.Groups("linkText").Value.ToLowerInvariant
                    linkText = linkText.Replace(vbCr, "").Replace(vbLf, "")

                    Dim linkAttributes As String = downloadLinksMatch.Groups("linkAttributes").Value

                    If linkText.ToLowerInvariant.Contains(kbNumberString.ToLowerInvariant) Then
                        Dim idMatch As Match = Regex.Match(linkAttributes, "id\=\'(?<id>[^\']+)\'", RegexOptions.IgnoreCase Or RegexOptions.Singleline Or RegexOptions.IgnorePatternWhitespace Or RegexOptions.ExplicitCapture)
                        If idMatch.Success AndAlso idMatch.Groups("id") IsNot Nothing AndAlso Not String.IsNullOrEmpty(idMatch.Groups("id").Value) Then
                            Dim patchGuid As String = idMatch.Groups("id").Value
                            If patchGuid.EndsWith("_link") Then
                                patchGuid = patchGuid.Replace("_link", "")
                                retVal.Add(patchGuid, linkText)
                            Else
                                LogError("Es konnte zu dem Update keine Downloads bestimmt werden. F2")
                            End If
                        Else
                            LogError("Es konnte zu dem Update keine Downloads bestimmt werden. F3")
                        End If

                    End If
                Else
                    LogError("Es konnte zu dem Update keine Downloads bestimmt werden. F4")
                End If
            Next
        Else
            If Not update.Equals("Summary of Intel microcode updates") Then
                LogError("Kb-Nummer konnte nicht bestimmt werden.")
            End If
        End If
        Return retVal
    End Function

    Private Function GetDownloadLink(patchGuid As String) As String
        Dim responseBody As String = ""
        Using client As New Net.WebClient
            Dim reqParm As New Specialized.NameValueCollection
            reqParm.Add("updateIDs", "[{""uidInfo"":""" + patchGuid + """,""updateID"":""" + patchGuid + """,""size"":0}]")

            Dim response As Byte() = client.UploadValues("https://www.catalog.update.microsoft.com/DownloadDialog.aspx", "POST", reqParm)
            responseBody = (New Text.UTF8Encoding).GetString(response)
        End Using

        If Not String.IsNullOrEmpty(responseBody) Then
            Dim bodyMatch As Match = Regex.Match(responseBody, "(?<url>http[s]?\:\/\/download\.windowsupdate\.com\/[^\'\""]*)", RegexOptions.IgnoreCase Or RegexOptions.Singleline Or RegexOptions.IgnorePatternWhitespace Or RegexOptions.ExplicitCapture)
            If bodyMatch.Success AndAlso bodyMatch.Groups("url") IsNot Nothing AndAlso Not String.IsNullOrEmpty(bodyMatch.Groups("url").Value) Then
                Return bodyMatch.Groups("url").Value
            Else
                LogError("Download-Link konnte nicht ermittelt werden.")
            End If
        Else
            LogError("Download-Link konnte nicht ermittelt werden.")
        End If

        Return Nothing
    End Function


    Private Function DownloadFile(destPath As String, downloadLink As String) As String
        Dim downloadLinkParts As String() = downloadLink.Split("/"c)
        Dim destFileName = New IO.FileInfo(downloadLinkParts(downloadLinkParts.Length - 1))
        Dim destFile As String = IO.Path.Combine(destPath, destFileName.Name)
        If IO.File.Exists(destFile) Then
            Console.WriteLine("**Datei bereits heruntergeladen")
            Return Nothing
        Else
            Dim checkSum As String = GetCheckSum(downloadLink)
            If (String.IsNullOrEmpty(checkSum)) Then
                LogError("**Keine Checksumme")
                Return Nothing
            End If

            Using client As New Net.WebClient
                client.DownloadFile(downloadLink, destFile)
            End Using

            If ValidateCheckSum(checkSum, destFile) Then
                Console.WriteLine("**Datei heruntergeladen")
                Return destFile
            Else
                IO.File.Delete(destFile)
                LogError("**Ungültige Checksumme")
                Return Nothing
            End If
        End If
    End Function



    Private Function GetCheckSum(url As String) As String
        Dim checkSumMatch As Match = Regex.Match(url, "_(?<checksum>[^.]+)\.", RegexOptions.IgnoreCase Or RegexOptions.Singleline Or RegexOptions.IgnorePatternWhitespace Or RegexOptions.ExplicitCapture)
        If checkSumMatch.Success AndAlso checkSumMatch.Groups("checksum") IsNot Nothing AndAlso Not String.IsNullOrEmpty(checkSumMatch.Groups("checksum").Value) Then
            Return checkSumMatch.Groups("checksum").Value
        Else
            LogError("**Checksumme nicht gefunden")
            Return Nothing
        End If
    End Function

    Private Function ValidateCheckSum(refCheckSum As String, file As String) As Boolean
        Dim checkSum As String = Nothing
        Using fileStream As IO.FileStream = IO.File.OpenRead(file)
            Using sha1 As System.Security.Cryptography.SHA1 = System.Security.Cryptography.SHA1.Create()
                checkSum = BitConverter.ToString(sha1.ComputeHash(fileStream)).Replace("-", "")
            End Using
        End Using
        Return refCheckSum.ToLowerInvariant().Equals(checkSum.ToLowerInvariant())
    End Function

    Private Sub InitializeDownloadFolder(downloadDir As String)

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

    End Sub

    Private Sub ApplyRegistryPatches()
        If IsAMD() Then
            Console.WriteLine("******RegistryPatch AMD******")
            Dim result As Integer = SpectrePatcherHelper.StartProcess("cmd.exe", "/c " & IO.Path.Combine(My.Application.Info.DirectoryPath & "\", "SpectreRegistryPatchAMD.cmd"))
            If result <> 0 Then
                LogError("Registry-Patch nicht angebracht")
            End If
        Else
            Console.WriteLine("******RegistryPatch Intel******")
            Dim result As Integer = SpectrePatcherHelper.StartProcess("cmd.exe", "/c " & IO.Path.Combine(My.Application.Info.DirectoryPath & "\", "SpectreRegistryPatchIntel.cmd"))
            If result <> 0 Then
                LogError("Registry-Patch nicht angebracht")
            End If
        End If

    End Sub
    Private Function DownloadAndInstallPatch(downloadId As String, downloadDir As String) As Boolean
        Dim downloadLink As String = GetDownloadLink(downloadId)

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
                        ElseIf exitCode = 2359302 Then
                            'Update bereits installiert.
                        ElseIf exitCode = 3010 Then
                            'Neustart erforderlich
                            Console.WriteLine("**Neustart erforderlich. Patchvorgang unvollständig")
                            Return False

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
        Return True
    End Function


    Public Sub PrintErrors(logFile As String)
        System.IO.File.AppendAllLines(logFile, ErrorLog)
        Console.WriteLine("Fertig")
        If ErrorLog.Count > 0 Then
            Environment.ExitCode = -1
            Console.WriteLine("Fehler: ")
            For Each errorStr As String In ErrorLog
                Console.WriteLine(errorStr)
            Next
        Else
            Environment.ExitCode = 0
        End If
        ErrorLog.Clear()
    End Sub

    Private Sub Reboot(logFile As String)
        PrintErrors(logFile)
        StartProcess("cmd", "/C shutdown -f -r -t 5")
    End Sub
End Class
