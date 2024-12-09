using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Registro_Proveedores
{
    public partial class Form1 : Form
    {
        private DataTable proveedoresData;
        private List<Registro> registros = new List<Registro>();
        private int numeroTurno = 1;
        private int ultimoID = 1;
        private readonly string archivoTurno = "ultimo_turno.txt";
        private readonly string archivoID = "ultimo_id.txt";
        private readonly string archivoRegistros = "registros.csv"; // Archivo para almacenar registros


        public Form1()
        {
            InitializeComponent();
            CargarDatosProveedores();
            InicializarTurno();
            InicializarID();
            CargarRegistros(); // Cargar registros desde el archivo
        }

        public class Registro
        {
            public int ID { get; set; }
            public int Turno { get; set; }
            public DateTime FechaHora { get; set; }
            public string Nombre { get; set; }
            public string Apellido { get; set; }
            public string Sociedad { get; set; }
            public string Proveedor { get; set; }
            public string Placas { get; set; }
        }

        // Inicializar el sistema de turnos
        private void InicializarTurno()
        {
            if (File.Exists(archivoTurno))
            {
                string[] contenido = File.ReadAllText(archivoTurno).Split('|');
                if (DateTime.TryParse(contenido[0], out DateTime ultimaFecha) && int.TryParse(contenido[1], out int ultimoTurno))
                {
                    numeroTurno = (ultimaFecha.Date == DateTime.Now.Date) ? ultimoTurno + 1 : 1;
                }
            }
            else
            {
                numeroTurno = 1; // Si el archivo no existe, inicia el turno en 1
            }
        }

        // Método para guardar un registro en el archivo CSV
        private void GuardarRegistroEnCSV(Registro registro)
        {
            bool existeArchivo = File.Exists(archivoRegistros);

            using (var writer = new StreamWriter(archivoRegistros, append: true))
            using (var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                if (!existeArchivo)
                {
                    csv.WriteHeader<Registro>(); // Escribir encabezado si el archivo no existe
                    csv.NextRecord();
                }
                csv.WriteRecord(registro);
                csv.NextRecord();
            }
        }

        // Método para cargar registros desde el archivo CSV
        private void CargarRegistros()
        {
            if (File.Exists(archivoRegistros))
            {
                using (var reader = new StreamReader(archivoRegistros))
                using (var csv = new CsvHelper.CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    registros = csv.GetRecords<Registro>().ToList();
                }
                ActualizarTabla(); // Actualizar la tabla con los registros cargados
            }
        }



        // Inicializar el ID único
        private void InicializarID()
        {
            if (File.Exists(archivoID) && int.TryParse(File.ReadAllText(archivoID), out int id))
            {
                ultimoID = id + 1;
            }
            else
            {
                ultimoID = 1; // Si el archivo no existe, inicia el ID en 1
            }
        }

        private void GuardarTurno()
        {
            File.WriteAllText(archivoTurno, $"{DateTime.Now:yyyy-MM-dd}|{numeroTurno}");
        }

        private void GuardarID()
        {
            File.WriteAllText(archivoID, ultimoID.ToString());
        }


        private void CargarDatosProveedores()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Proveedores.xlsx");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("El archivo 'Proveedores.xlsx' no se encuentra en el directorio del proyecto.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            proveedoresData = new DataTable();
            try
            {
                OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial; // Configurar licencia EPPlus

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0]; // Primera hoja
                    int rows = worksheet.Dimension.Rows;

                    // Crear listas para almacenar valores únicos
                    HashSet<string> sociedades = new HashSet<string>();
                    HashSet<string> proveedores = new HashSet<string>();

                    // Agregar filas a las listas únicas
                    for (int row = 2; row <= rows; row++) // Comienza desde la fila 2 para ignorar los encabezados
                    {
                        string sociedad = worksheet.Cells[row, 1].Text.Trim(); // Columna A
                        string proveedor = worksheet.Cells[row, 4].Text.Trim(); // Columna D

                        if (!string.IsNullOrWhiteSpace(sociedad)) sociedades.Add(sociedad);
                        if (!string.IsNullOrWhiteSpace(proveedor)) proveedores.Add(proveedor);
                    }

                    // Configurar ComboBox Sociedad
                    comboBoxSociedad.DataSource = new List<string>(sociedades); // Lista única
                    comboBoxSociedad.SelectedIndex = -1; // Inicializar vacío

                    // Configurar ComboBox Proveedores con datos únicos
                    comboBoxProveedores.DataSource = new List<string>(proveedores); // Lista única
                    comboBoxProveedores.SelectedIndex = -1; // Inicializar vacío
                    comboBoxProveedores.AutoCompleteMode = AutoCompleteMode.SuggestAppend; // Activar sugerencias
                    comboBoxProveedores.AutoCompleteSource = AutoCompleteSource.ListItems;
                    comboBoxProveedores.TextChanged += ComboBoxProveedores_TextChanged; // Evento para buscar dinámicamente
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el archivo de proveedores: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ComboBoxProveedores_TextChanged(object sender, EventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            if (comboBox == null || proveedoresData == null) return;

            string searchText = comboBox.Text.ToLower();
            List<string> filteredList = new List<string>();

            foreach (DataRow row in proveedoresData.Rows)
            {
                string proveedor = row["Proveedor"].ToString();
                if (proveedor.ToLower().Contains(searchText)) // Filtrar por coincidencias parciales
                {
                    filteredList.Add(proveedor);
                }
            }

            if (filteredList.Count > 0)
            {
                comboBox.DataSource = filteredList;
                comboBox.DroppedDown = true; // Mostrar la lista desplegable automáticamente
                comboBox.SelectionStart = searchText.Length; // Mantener el cursor al final del texto
            }
            else
            {
                comboBox.DroppedDown = false; // Ocultar si no hay resultados
            }
        }


        private void btnRegistrar_Click(object sender, EventArgs e)
        {
            // Validar campos
            if (string.IsNullOrWhiteSpace(textBoxNombre.Text.Trim()))
            {
                MessageBox.Show("Por favor, ingrese el nombre.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(textBoxApellido.Text.Trim()))
            {
                MessageBox.Show("Por favor, ingrese el apellido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (comboBoxSociedad.SelectedItem == null || string.IsNullOrWhiteSpace(comboBoxSociedad.SelectedItem.ToString()))
            {
                MessageBox.Show("Por favor, seleccione una sociedad.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (comboBoxProveedores.SelectedItem == null || string.IsNullOrWhiteSpace(comboBoxProveedores.SelectedItem.ToString()))
            {
                MessageBox.Show("Por favor, seleccione un proveedor.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(textBoxPlacas.Text.Trim()))
            {
                MessageBox.Show("Por favor, ingrese las placas.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Crear un nuevo registro
            Registro nuevoRegistro = new Registro
            {
                ID = ultimoID++,
                Turno = numeroTurno++,
                FechaHora = DateTime.Now,
                Nombre = textBoxNombre.Text.Trim(),
                Apellido = textBoxApellido.Text.Trim(),
                Sociedad = comboBoxSociedad.SelectedItem.ToString(),
                Proveedor = comboBoxProveedores.SelectedItem.ToString(),
                Placas = textBoxPlacas.Text.Trim()
            };

            // Guardar registro
            registros.Add(nuevoRegistro);
            GuardarRegistroEnCSV(nuevoRegistro); // Guardar el registro en el archivo CSV
            ActualizarTabla();
            EscribirEnGoogleSheets(nuevoRegistro);
            LimpiarCampos();

            // Guardar estado del turno y ID
            GuardarTurno();
            GuardarID();

            MessageBox.Show($"Registro completado. Turno: {nuevoRegistro.Turno} | ID: {nuevoRegistro.ID}", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ImprimirTicket(nuevoRegistro);
        }




        private void ActualizarTabla()
        {
            var registrosOrdenados = registros.OrderByDescending(r => r.ID).ToList(); // Ordenar por ID descendente
            dataGridView1.DataSource = null; // Limpiar la fuente de datos
            dataGridView1.DataSource = new BindingList<Registro>(registrosOrdenados); // Asignar la lista ordenada
        }



        private void LimpiarCampos()
        {
            textBoxNombre.Clear();
            textBoxApellido.Clear();
            comboBoxSociedad.SelectedIndex = -1;
            comboBoxProveedores.SelectedIndex = -1;
            textBoxPlacas.Clear();
        }




        private void ImprimirTicket(Registro registro)
        {
            PrintDocument printDocument = new PrintDocument();
            printDocument.PrintPage += (sender, e) =>
            {
                using (Font titleFont = new Font("Arial", 16, FontStyle.Bold))
                using (Font contentFont = new Font("Arial", 12))
                {
                    int y = 10;

                    e.Graphics.DrawString("Registro de Proveedor", titleFont, Brushes.Black, 10, y);
                    y += 40;

                    e.Graphics.DrawString($"ID: {registro.ID}", contentFont, Brushes.Black, 10, y); y += 25;
                    e.Graphics.DrawString($"Turno: {registro.Turno}", contentFont, Brushes.Black, 10, y); y += 25;
                    e.Graphics.DrawString($"Fecha y Hora: {registro.FechaHora:yyyy-MM-dd HH:mm:ss}", contentFont, Brushes.Black, 10, y); y += 25;
                    e.Graphics.DrawString($"Nombre: {registro.Nombre}", contentFont, Brushes.Black, 10, y); y += 25;
                    e.Graphics.DrawString($"Apellido: {registro.Apellido}", contentFont, Brushes.Black, 10, y); y += 25;
                    e.Graphics.DrawString($"Sociedad: {registro.Sociedad}", contentFont, Brushes.Black, 10, y); y += 25;
                    e.Graphics.DrawString($"Proveedor: {registro.Proveedor}", contentFont, Brushes.Black, 10, y); y += 25;
                    e.Graphics.DrawString($"Placas: {registro.Placas}", contentFont, Brushes.Black, 10, y); y += 40;

                    e.Graphics.DrawString("¡Gracias por su visita!", titleFont, Brushes.Black, 10, y);
                }
            };

            printDocument.Print();
        }

        private void EscribirEnGoogleSheets(Registro registro)
        {
            string credencialesJsonPath = "credentials.json";
            string spreadsheetId = "1g2O40Tkq4g0_Zb55moGelWop5aQdAqVriKi30UtjLfk";
            string sheetName = "Hoja 1";

            if (!File.Exists(credencialesJsonPath))
            {
                MessageBox.Show("El archivo de credenciales no se encuentra.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
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

                var service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Registro_Proveedores",
                });

                var values = new List<IList<object>> {
                    new List<object> {
                        registro.ID,
                        registro.Turno,
                        registro.FechaHora.ToString("yyyy-MM-dd HH:mm:ss"),
                        registro.Nombre,
                        registro.Apellido,
                        registro.Sociedad,
                        registro.Proveedor,
                        registro.Placas
                    }
                };

                var valueRange = new ValueRange { Values = values };
                var appendRequest = service.Spreadsheets.Values.Append(valueRange, spreadsheetId, $"{sheetName}!A:A");
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
                appendRequest.Execute();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar en Google Sheets: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
