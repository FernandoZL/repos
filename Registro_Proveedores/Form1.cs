using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;



namespace Registro_Proveedores
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public class Registro
        {
            public int Turno { get; set; }
            public DateTime FechaHora { get; set; }
            public string Nombre { get; set; }
            public string Apellido { get; set; }
            public string Licencia { get; set; }
            public string Proveedor { get; set; }
            public string Placas { get; set; }
        }

        private int numeroTurno = 1; // Contador de turnos
        private List<Registro> registros = new List<Registro>(); // Lista de registros


        private void btnRegistrar_Click(object sender, EventArgs e)
        {
            // Validar campos
            if (string.IsNullOrWhiteSpace(textBoxNombre.Text) ||
                string.IsNullOrWhiteSpace(textBoxApellido.Text) ||
                string.IsNullOrWhiteSpace(textBoxLicencia.Text) ||
                string.IsNullOrWhiteSpace(textBoxProveedor.Text) ||
                string.IsNullOrWhiteSpace(textBoxPlacas.Text))
            {
                MessageBox.Show("Por favor, complete todos los campos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Crear un nuevo registro
            Registro nuevoRegistro = new Registro
            {
                Turno = numeroTurno++,
                FechaHora = DateTime.Now,
                Nombre = textBoxNombre.Text,
                Apellido = textBoxApellido.Text,
                Licencia = textBoxLicencia.Text,
                Proveedor = textBoxProveedor.Text,
                Placas = textBoxPlacas.Text
            };

            // Guardar el registro en la lista
            registros.Add(nuevoRegistro);

            // Actualizar la tabla
            ActualizarTabla();

            // Guardar en Google Sheets
            EscribirEnGoogleSheets(nuevoRegistro);

            // Limpiar los campos
            LimpiarCampos();

            // Mostrar un mensaje de éxito
            MessageBox.Show($"Registro completado. Turno: {nuevoRegistro.Turno}", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ActualizarTabla()
        {
            dataGridViewRegistros.DataSource = null; // Limpiar la tabla
            dataGridViewRegistros.DataSource = registros; // Recargar datos
        }

        private void LimpiarCampos()
        {
            textBoxNombre.Clear();
            textBoxApellido.Clear();
            textBoxLicencia.Clear();
            textBoxProveedor.Clear();
            textBoxPlacas.Clear();
        }




private void EscribirEnGoogleSheets(Registro registro)
    {
        string credencialesJsonPath = "credentials.json";
        string spreadsheetId = "1g2O40Tkq4g0_Zb55moGelWop5aQdAqVriKi30UtjLfk";
        string sheetName = "Hoja1";

        // Verificar si el archivo de credenciales existe
        if (!File.Exists(credencialesJsonPath))
        {
            MessageBox.Show("El archivo de credenciales no se encontró. Asegúrate de que 'credentials.json' esté en la carpeta del proyecto.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        UserCredential credential;
        using (var stream = new FileStream(credencialesJsonPath, FileMode.Open, FileAccess.Read))
        {
            string[] scopes = { SheetsService.Scope.Spreadsheets };
            credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                scopes,
                "user",
                CancellationToken.None,
                new FileDataStore("GoogleSheetsToken", true)
            ).Result;
        }

        var sheetsService = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "Registro_Proveedores",
        });

        var values = new List<IList<object>> {
        new List<object> {
            registro.Turno,
            registro.FechaHora.ToString("yyyy-MM-dd HH:mm:ss"),
            registro.Nombre,
            registro.Apellido,
            registro.Licencia,
            registro.Proveedor,
            registro.Placas
        }
    };

        var valueRange = new ValueRange
        {
            Values = values
        };

        string range = $"{sheetName}!A:A";

        var appendRequest = sheetsService.Spreadsheets.Values.Append(valueRange, spreadsheetId, range);
        appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;

        appendRequest.Execute();
    }



}
}
