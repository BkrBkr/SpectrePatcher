# SpectrePatcher
SpectrePatcher is a tool to automatically download and install the Intel microcode updates released by Microsoft.
SpectrePatcher dient dazu die von Microsoft veröffentlichten Microcode-Updates automatisiert herunterzuladen und zu installieren.

Microsoft hat eine Reihe von Patches veröffentlicht, die Microcode-Updates gegen die Sicherheitslücke Spectre enthalten. Diese werden jedoch nicht per Windows-Update verteilt. Dieses Tool stellt sicher, dass die nötigen Updates automatisch installiert werden. Sollte Microsoft in Zukunft weitere Patches veröffentlichen werden diese automatisch nachinstalliert.

# Installation
Das ZIP-Archiv kann einfach entpackt und die Datei "SpectrePatcher.exe" ausgeführt werden. Das .NET Framework wird vorausgesetzt. Die Anwendung installiert dann einmalig die notwendigen Updates. Es empfiehlt sich einen geplanten Task in der Windows Aufgabenplanung einzurichten, um die Anwendung regelmäßig auszuführen.

# Hinweis zu Virenscannern
Diese Anwendung lädt die notwendigen Patches aus dem Internet nach und führt diese aus. Diese Verhalten ähnelt dem einiger Schadsoftware. Daher kann es unter Umständen zu einer fehlerhaften Erkennung durch den Virenscanner kommen. Diese Anwendung enthält selbstverständlich keine schädlichen Komponenten und lädt Updates nur von der offiziellen Microsoft Webseite.

# Hilfe und Kontakt
Bei Fragen und Problemen bitte einen "Issue" in Github anlegen.

# Haftung
SpectrePatcher wird in der Hoffnung, dass es nützlich sein wird, aber OHNE JEDE GEWÄHRLEISTUNG, bereitgestellt; sogar ohne die implizite Gewährleistung der MARKTFÄHIGKEIT oder EIGNUNG FÜR EINEN BESTIMMTEN ZWECK. Siehe die GNU General Public License für weitere Details.

SpectrePatcher Is distributed In the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY Or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License For more details.
