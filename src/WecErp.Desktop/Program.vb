Imports System
Imports System.Windows.Forms

Module Program
    <STAThread>
    Sub Main()
        ApplicationConfiguration.Initialize()
        Application.Run(New MainForm())
    End Sub
End Module
