using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.IO;

namespace DESCrypterWindowsForms
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            toolStripComboBox1.SelectedIndex = 0;
            toolStripComboBox2.SelectedIndex = 0;
        }
        DESCryptoServiceProvider DES = new DESCryptoServiceProvider();
        byte[] raw_data;
        byte[] encrypted_data;

        private void создатьКлючToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Сохранение ключа";
            sfd.RestoreDirectory = true;
            sfd.DefaultExt = ".bin";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllBytes(sfd.FileName, DES.Key);
                MessageBox.Show("Ключ успешно сохранен", "Создание ключа", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void загрузитьКлючToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Загрузка ключа";
            ofd.Filter = "key files (*.bin)|*.bin|All files (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                // Проверка, является ли выбранный файл ключом
                try
                {
                    byte[] key = new byte[8];
                    key = File.ReadAllBytes(ofd.FileName);
                    DES.Key = key;
                    MessageBox.Show("Ключ успешно загружен", "Загрузка ключа", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (System.Exception)
                {
                    // Если файл не является ключом, то выводится сообщение об ошибке
                    MessageBox.Show("Выбранный файл не является ключом", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                
            }

        }
        

        private void отобразитьШифрованнуюИнформациюToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Выбор зашифрованного файла";
            ofd.Filter = "crypt files (*.crypt)|*.crypt|All files (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                raw_data = File.ReadAllBytes(ofd.FileName);
                cryptedTextBox.Text = Encoding.UTF8.GetString(raw_data);
            }
        }


        private void encryptButton_Click(object sender, EventArgs e)
        {
            if (decryptedTextBox.Text != "")
            {
                raw_data = Encoding.UTF8.GetBytes(decryptedTextBox.Text);
                encrypted_data = DESEncryptionDecryption.crypt(DES, raw_data);
                cryptedTextBox.Text = Encoding.UTF8.GetString(encrypted_data);
            } else
            {
                MessageBox.Show("Сначала введите текст в поле", "Шифрование", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }

        private void decryptButton_Click(object sender, EventArgs e)
        {
            if (cryptedTextBox.Text != "" && encrypted_data.Length != 0)
            {
                try {
                    raw_data = DESEncryptionDecryption.decrypt(DES, encrypted_data);
                }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    raw_data = null;
                    MessageBox.Show("Выбран неправильный ключ или режим!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                decryptedTextBox.Text = Encoding.UTF8.GetString(raw_data);
            }
            else
            {
                MessageBox.Show("Сначала загрузите зашифрованный файл", "Дешифрование", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void saveCryptedButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Сохранение зашифрованных данных";
            sfd.DefaultExt = ".crypt";
            if (sfd.ShowDialog() == DialogResult.OK) File.WriteAllBytes(sfd.FileName, encrypted_data);
        }

        private void saveDecryptedButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Сохранение расшифрованных данных";
            sfd.DefaultExt = ".txt";
            if (sfd.ShowDialog() == DialogResult.OK) File.WriteAllText(sfd.FileName, decryptedTextBox.Text);
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (toolStripComboBox1.SelectedIndex)
            {
                case 0: DES.Mode = CipherMode.CBC; break;
                case 1: DES.Mode = CipherMode.CFB; break;        
                case 2: DES.Mode = CipherMode.ECB; break;
                case 3: DES.Mode = CipherMode.OFB; break;
            }
        }


        private void поточноеШифрованиеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Загрузить файл";
            if (ofd.ShowDialog() != DialogResult.OK) return;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Сохранить в файл";
            if (sfd.ShowDialog() != DialogResult.OK) return;

            const int blockSize = 1024;
            ICryptoTransform transform = DES.CreateEncryptor();

            FileStream inFileStream = new FileStream(ofd.FileName, FileMode.Open);
            FileStream outFileStream = new FileStream(sfd.FileName, FileMode.OpenOrCreate);

            // Проверка кратности для режима без дополнений
            if (DES.Padding == PaddingMode.None & inFileStream.Length % 64 != 0)
            {
                MessageBox.Show("Входные данные не кратны 64 битам!", "Шифрование без дополнений", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            outFileStream.Write(DES.IV, 0, DES.IV.Length);
            try
            {
                using (CryptoStream cs = new CryptoStream(outFileStream, transform, CryptoStreamMode.Write))
                {
                    byte[] buffer = new byte[blockSize];
                    int bytesRead;

                    // Потоковое чтение и запись файла по кускам
                    while ((bytesRead = inFileStream.Read(buffer, 0, blockSize)) > 0) cs.Write(buffer, 0, bytesRead);
                    cs.Flush();
                    cs.FlushFinalBlock();

                }
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                inFileStream.Close();
                outFileStream.Close();
                MessageBox.Show("Выбран неправильный ключ или режим!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            inFileStream.Close();
            outFileStream.Close();
            encrypted_data = File.ReadAllBytes(sfd.FileName);
            cryptedTextBox.Text = Encoding.UTF8.GetString(encrypted_data);
            MessageBox.Show("Файл успешно зашифрован!", "Поточное шифрование", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        private void шифрованиеВПамятиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Выбор файла";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                // Чтение файла в массив байтов
                raw_data = File.ReadAllBytes(ofd.FileName);

                // Проверка кратности для режима без дополнений
                if (DES.Padding == PaddingMode.None & raw_data.Length % 64 != 0)
                {
                    raw_data = null;
                    MessageBox.Show("Входные данные не кратны 64 битам!", "Шифрование без дополнений", MessageBoxButtons.OK, MessageBoxIcon.Error); 
                    return;
                }

                try
                {
                    // Шифрование данных
                    encrypted_data = DESEncryptionDecryption.crypt(DES, raw_data);
                }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    raw_data = null;
                    MessageBox.Show("Выбран неправильный ключ или режим!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // Вывод зашифрованных данных в текстовое поле
                cryptedTextBox.Text = Encoding.UTF8.GetString(encrypted_data);
                // Сохранение зашифрованных данных в файл
                File.WriteAllBytes(ofd.FileName + ".crypt", encrypted_data);
                MessageBox.Show("Файл успешно зашифрован!", "Шифрование", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void дешифрованиеВПамятиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Выбор зашифрованного файла";
            ofd.Filter = "crypt files (*.crypt)|*.crypt|All files (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                // Чтение файла в массив байтов
                encrypted_data = File.ReadAllBytes(ofd.FileName);
                try
                {
                    // Расшифрование данных
                    raw_data = DESEncryptionDecryption.decrypt(DES, encrypted_data);
                }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    encrypted_data = null;
                    MessageBox.Show("Выбран неправильный ключ или режим!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // Вывод расшифрованных данных в текстовое поле
                decryptedTextBox.Text = Encoding.UTF8.GetString(raw_data);
                MessageBox.Show("Файл успешно расшифрован!", "Дешифрование", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void поточноеДешифрованиеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Выбор зашифрованного файла";
            ofd.Filter = "crypt files (*.crypt)|*.crypt|All files (*.*)|*.*";
            if (ofd.ShowDialog() != DialogResult.OK) return;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Сохранить в файл";
            if (sfd.ShowDialog() != DialogResult.OK) return;

            FileStream inFileStream = new FileStream(ofd.FileName, FileMode.Open);
            FileStream outFileStream = new FileStream(sfd.FileName, FileMode.OpenOrCreate);

            // Проверка кратности для режима без дополнений
            if (DES.Padding == PaddingMode.None & inFileStream.Length % 64 != 0)
            {
                MessageBox.Show("Входные данные не кратны 64 битам!", "Шифрование без дополнений", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Byte[] iv = new byte[DES.IV.Length];
            inFileStream.Read(iv, 0, iv.Length);
            DES.IV = iv;

            const int blockSize = 1024;
            ICryptoTransform transform = DES.CreateDecryptor();
            try
            {
                using (CryptoStream cs = new CryptoStream(outFileStream, transform, CryptoStreamMode.Write))
                {
                    byte[] buffer = new byte[blockSize];
                    int bytesRead;

                    // Потоковое чтение и запись файла по кускам

                    while ((bytesRead = inFileStream.Read(buffer, 0, blockSize)) > 0)
                    {
                        cs.Write(buffer, 0, bytesRead);
                    }

                    cs.Flush();
                    cs.FlushFinalBlock();
                }
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                inFileStream.Close();
                outFileStream.Close();
                MessageBox.Show("Выбран неправильный ключ или режим!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            inFileStream.Close();
            outFileStream.Close();
            raw_data = File.ReadAllBytes(sfd.FileName);
            decryptedTextBox.Text = Encoding.UTF8.GetString(raw_data);
            MessageBox.Show("Файл успешно расшифрован!", "Поточное дешифрование", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void toolStripComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (toolStripComboBox2.SelectedIndex)
            {
                case 0: DES.Padding = PaddingMode.ISO10126; break;
                case 1: 
                    DES.Padding = PaddingMode.None;
                    MessageBox.Show("В режиме без дополнений размер входных данных должен быть кратен 64 битам!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                case 2: DES.Padding = PaddingMode.PKCS7; break;
                case 3: DES.Padding = PaddingMode.Zeros; break;
            }


        }
    }
    public class DESEncryptionDecryption{
        public static byte[] crypt(DESCryptoServiceProvider DES, byte[] data)
        {
            byte[] crypted_data;
            ICryptoTransform transform = DES.CreateEncryptor();
            using (MemoryStream ms = new MemoryStream())
            {
                // Запись случайного вектора инициализации без его шифрования
                ms.Write(DES.IV, 0, DES.IV.Length);
                // Создание криптографического потока в режиме записи
                using (CryptoStream cs = new CryptoStream(ms, transform, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.Flush();
                    cs.FlushFinalBlock();
                }
                crypted_data = ms.ToArray();
            }
            return crypted_data;
        }
        public static byte[] decrypt(DESCryptoServiceProvider DES, byte[] data)
        {
            byte[] decrypted_data;
            using (MemoryStream ms = new MemoryStream(data))
            {
                // Чтение вектора инициализации из потока
                Byte[] iv = new byte[DES.IV.Length];
                ms.Read(iv, 0, iv.Length);
                DES.IV = iv;
                // Создание интерфейса преобразования для дешифрования по алгоритму DES
                ICryptoTransform transform = DES.CreateDecryptor();
                // Создание криптографического потока в режиме записи. Этот поток будет
                // декодировать двоичные данные сразу после их чтения из потока
                using (CryptoStream cs = new CryptoStream(ms, transform, CryptoStreamMode.Read))
                {
                    decrypted_data = new byte[ms.Length];
                    cs.Read(decrypted_data, 0, decrypted_data.Length);
                }
            return decrypted_data;
            }

        }
    }
}
