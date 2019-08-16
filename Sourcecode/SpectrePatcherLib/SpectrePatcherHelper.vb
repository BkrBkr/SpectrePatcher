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
Imports System.Management
Imports System.Net
Imports System.Text.RegularExpressions
Imports System.Xml

Public Class SpectrePatcherHelper
    Public ReadOnly Property ErrorLog As New List(Of String)

    Public Sub LogError(msg As String)
        _ErrorLog.Add(msg)
    End Sub
    Public Sub LogError(ex As Exception)
        _ErrorLog.Add(ex.Message)
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

    Public Function IsAMD() As Boolean
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

    Public Function GetUpdateList() As List(Of String)
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


    Public Function SearchDownloadsForUpdate(update As String) As Dictionary(Of String, String)
        Dim retVal As New Dictionary(Of String, String)

        Dim kbNumberMatch As Match = Regex.Match(update, "(?<number>KB[0-9]{4,})", RegexOptions.IgnoreCase Or RegexOptions.Singleline Or RegexOptions.IgnorePatternWhitespace Or RegexOptions.ExplicitCapture)

        If kbNumberMatch IsNot Nothing AndAlso kbNumberMatch.Success AndAlso kbNumberMatch.Groups("number") IsNot Nothing AndAlso Not String.IsNullOrEmpty(kbNumberMatch.Groups("number").Value) Then

            Dim kbNumberString As String = kbNumberMatch.Groups("number").Value
            Dim searchResult As String = ""
            Using updateSearchClient As New WebClient()
                searchResult = updateSearchClient.DownloadString("https://www.catalog.update.microsoft.com/Search.aspx?q=" & kbNumberString)
            End Using
            Dim downloadLinksMatches As MatchCollection = Regex.Matches(searchResult, "<a\s+(?<linkAttributes>[^>]+)>(?<linkText>.+?)<\/a>", RegexOptions.IgnoreCase Or RegexOptions.Singleline Or RegexOptions.IgnorePatternWhitespace Or RegexOptions.ExplicitCapture)
            If downloadLinksMatches.Count = 0 Then
                LogError("Es konnte zu dem Update keine Downloads bestimmt werden. F1")
            End If

            For Each downloadLinksMatch As Match In downloadLinksMatches
                If downloadLinksMatch.Success AndAlso downloadLinksMatch.Groups("linkAttributes") IsNot Nothing AndAlso downloadLinksMatch.Groups("linkText") IsNot Nothing Then
                    Dim linkText As String = downloadLinksMatch.Groups("linkText").Value.ToLowerInvariant
                    Dim linkAttributes As String = downloadLinksMatch.Groups("linkAttributes").Value

                    linkText = linkText.Replace(vbCr, "").Replace(vbLf, "")

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

    Public Function GetDownloadLink(patchGuid As String) As String
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


    Public Function DownloadFile(destPath As String, downloadLink As String) As String
        Dim downloadLinkParts As String() = downloadLink.Split("/"c)
        Dim destFile As String = IO.Path.Combine(destPath, downloadLinkParts(downloadLinkParts.Length - 1))
        If IO.File.Exists(destFile) Then
            Console.WriteLine("**Datei bereits heruntergeladen")
            Return Nothing
        Else
            Dim checkSum As String = GetCheckSum(downloadLink)
            If (String.IsNullOrEmpty(checkSum)) Then
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



    Public Function GetCheckSum(url As String) As String
        Dim checkSumMatch As Match = Regex.Match(url, "_(?<checksum>[^.]+)\.", RegexOptions.IgnoreCase Or RegexOptions.Singleline Or RegexOptions.IgnorePatternWhitespace Or RegexOptions.ExplicitCapture)
        If checkSumMatch.Success AndAlso checkSumMatch.Groups("checksum") IsNot Nothing AndAlso Not String.IsNullOrEmpty(checkSumMatch.Groups("checksum").Value) Then
            Return checkSumMatch.Groups("checksum").Value
        Else
            LogError("**Checksumme nicht gefunden")
            Return Nothing
        End If
    End Function

    Public Function ValidateCheckSum(refCheckSum As String, file As String) As Boolean
        Dim checkSum As String = Nothing
        Using fileStream As IO.FileStream = IO.File.OpenRead(file)
            Using sha1 As System.Security.Cryptography.SHA1 = System.Security.Cryptography.SHA1.Create()
                checkSum = BitConverter.ToString(sha1.ComputeHash(fileStream)).Replace("-", "")
            End Using
        End Using
        Return refCheckSum.ToLowerInvariant().Equals(checkSum.ToLowerInvariant())
    End Function
End Class
