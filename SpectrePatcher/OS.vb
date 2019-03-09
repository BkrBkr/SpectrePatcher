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
Imports System.Runtime.InteropServices

Public Class OS
    Public Shared Function IsWindowsServer() As Boolean
        Return OS.IsOS(OS.OS_ANYSERVER)
    End Function

    Const OS_ANYSERVER As Integer = 29
    <DllImport("shlwapi.dll", SetLastError:=True, EntryPoint:="#437")>
    Private Shared Function IsOS(ByVal os As Integer) As Boolean

    End Function
End Class
