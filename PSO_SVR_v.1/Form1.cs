/*
 * lbest PSO_SVR v1.0
 * Dinda Novitasari
 * 115060800111007
 * Informatika/ Ilmu Komputer
 * Universitas Brawijaya
 * id.dindanovitasari@gmail.com
 * labworks.dindanovitasari.com
 */
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Diagnostics;
using MySql.Data.MySqlClient;

namespace PSO_SVR_v._1
{
    public partial class Form1 : Form
    {
        double c1, c2, maxInersia, minInersia, inertia, gamma;
        int baris, kolom, fitur, iterasi, i, fold, k, jumlahPartikel, maxIterasi, dBiner, jumlahData, dKontinu = 5;
        int[] uji, latih, count;
        double[,] dataset, dataset_normal, dataset_acak, partikel, pBest, gBest, solusi, velocity, matriks_regresi, lBest, tetangga;
        double[] max, min, vMax, error, fx, alpha, alpha_star, batasAtas, batasBawah, euclidian_partikel;
        double[][,] fold_dataset_latih, fold_dataset_uji, fold_dataset_latih_seleksi, fold_dataset_uji_seleksi;
        Random acak = new Random();
        string connectionSQL = "server=localhost;database=data_see;uid=root;password=;";
        
        public Form1()
        {
            InitializeComponent();
            parameter_pso();
            ruang_pencarian();
            load_data();
            load_dataset(dataset);
            load_dimensi();
            load_fold();
            load_inersia();
            load_aksel();
            load_jPartikel();
            load_mIterasi();
        }
        public class Hasil
        {
            public int Iterasi { get; set; }
            public double C { get; set; }
            public double Epsilon { get; set; }
            public double Sigma { get; set; }
            public double cLR { get; set; }
            public double Lambda { get; set; }
            public double F1 { get; set; }
            public double F2 { get; set; }
            public double F3 { get; set; }
            public double F4 { get; set; }
            public double F5 { get; set; }
            public double F6 { get; set; }
            public double F7 { get; set; }
            public double Cost { get; set; }
        }
        public class headerData
        {
            public int No { get; set; }
            public double TeamExp { get; set; }
            public double ManagerExp { get; set; }
            public double Transaction { get; set; }
            public double Entities { get; set; }
            public double PointAdjust { get; set; }
            public double Envergure { get; set; }
            public double PointNonAdjust { get; set; }
            public double Effort { get; set; }
        }
        private void proses_Click(object sender, EventArgs e)
        {
            var watch = Stopwatch.StartNew();
            set_parameter_pso();
            set_ruang_pencarian();
            normalisasi_data();
            solusi = pso_svr();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            this.Text = "PSO_SVR 1.0 | Iterasi Ke- " + (iterasi-1).ToString() + " dari " + maxIterasi.ToString() +" | Waktu Komputasi: "+elapsedMs.ToString() + " ms";
            load_tabel(solusi,dataGridView2);
        }
        void parameter_pso()
        {
            textBox1.Text = "10";
            textBox2.Text = "10";
            textBox3.Text = "2";
            textBox4.Text = "2";
            textBox5.Text = "0,8";
            textBox6.Text = "0,4";
            textBox7.Text = "3";
        }
        void ruang_pencarian()
        {
            textBox8.Text = "0,01";//c bawah
            textBox9.Text = "1000";//c atas
            textBox10.Text = "0,01";//epsilon bawah
            textBox11.Text = "0,09";//epsilon atas
            textBox12.Text = "0,01";//sigma bawah
            textBox13.Text = "10";//sigma atas
            textBox14.Text = "0,01";//LR bawah
            textBox15.Text = "2";//LR atas
            textBox16.Text = "0,01";//Lambda bawah
            textBox17.Text = "5";//Lambda atas
        }
        void set_parameter_pso()
        {
            jumlahPartikel = int.Parse(textBox1.Text);
            maxIterasi = int.Parse(textBox2.Text);
            c1 = double.Parse(textBox3.Text);
            c2 = double.Parse(textBox4.Text);
            maxInersia = double.Parse(textBox5.Text);
            minInersia = double.Parse(textBox6.Text);
            k = int.Parse(textBox7.Text);
        }
        void set_ruang_pencarian()
        {
            batasAtas = new double[dKontinu];
            batasBawah = new double[dKontinu];
            batasBawah[0] = double.Parse(textBox8.Text);
            batasAtas[0] = double.Parse(textBox9.Text);
            batasBawah[1] = double.Parse(textBox10.Text);
            batasAtas[1] = double.Parse(textBox11.Text);
            batasBawah[2] = double.Parse(textBox12.Text);
            batasAtas[2] = double.Parse(textBox13.Text);
            batasBawah[3] = double.Parse(textBox14.Text);
            batasAtas[3] = double.Parse(textBox15.Text);
            batasBawah[4] = double.Parse(textBox16.Text);
            batasAtas[4] = double.Parse(textBox17.Text);
        }
        void Shuffle<T>(T[] array)
        {
            /*
             Proses Mengacak Urutan Data
             * Proses ini menggunakan algoritma Fisher Yates
             */
            int n = array.Length;
            for (int i_ = 0; i_ < n; i_++)
            {
                int r = i_ + (int)(acak.NextDouble() * (n - i_));
                T t = array[r];
                array[r] = array[i_];
                array[i_] = t;
            }
        }
        void load_data()
        {
            //fungsi load_data() digunakan untuk load data dari database
            //string connectionSQL = "server=localhost;database=data_see;uid=root;password=;";
            //========menghitung kolom
            string kolomz = null;
            string sql1 = "SELECT COUNT( * ) FROM INFORMATION_SCHEMA.COLUMNS WHERE table_schema ='data_see'AND table_name =  'data_proyek';";
            MySqlConnection db1 = new MySqlConnection(connectionSQL);
            MySqlCommand dbcmd1 = new MySqlCommand(sql1, db1);
            try
            {
                db1.Open();
                MySqlDataReader sqlReader = dbcmd1.ExecuteReader();
                if (sqlReader.HasRows)
                {
                    while (sqlReader.Read())
                    {
                        kolomz = sqlReader[0].ToString();
                    }
                }
                db1.Close();
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Maaf, terjadi kesalahan karena " + kesalahan);
                Console.WriteLine("Error: {0}", ex.ToString());
            }
            int jumlahAtribut = int.Parse(kolomz);
            Console.WriteLine(jumlahAtribut);
            dBiner = jumlahAtribut - 2;
            //Console.WriteLine("Biner: {0}", dBiner);

            //==============menghitung jumlah data
            string s_jumlahData = null;
            string sql2 = "SELECT count(*) FROM data_proyek;";
            MySqlConnection db2 = new MySqlConnection(connectionSQL);
            MySqlCommand dbcmd2 = new MySqlCommand(sql2, db2);
            try
            {
                db2.Open();
                MySqlDataReader sqlReader = dbcmd2.ExecuteReader();
                if (sqlReader.HasRows)
                {
                    while (sqlReader.Read())
                    {
                        s_jumlahData = sqlReader[0].ToString();
                    }
                }
                db2.Close();
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Maaf, terjadi kesalahan karena " + kesalahan);
                Console.WriteLine("Error: {0}", ex.ToString());
            }
            jumlahData = int.Parse(s_jumlahData);
            //Console.WriteLine("jumlah data: {0}", jumlahData);

            //==============load data
            string sql3 = "select * from data_proyek";
            string[,] simp = new string[jumlahData, jumlahAtribut];
            double [,] _dataset=new double[jumlahData, jumlahAtribut];
            dataset = new double[jumlahData, jumlahAtribut-1];
            int i = 0;
            MySqlConnection db3 = new MySqlConnection(connectionSQL);
            MySqlCommand dbcmd3 = new MySqlCommand(sql3, db3);
            try
            {
                db3.Open();
                MySqlDataReader sqlReader = dbcmd3.ExecuteReader();
                if (sqlReader.HasRows)
                {
                    while (sqlReader.Read())
                    {
                        for (int j = 0; j < jumlahAtribut; j++)
                        {
                            simp[i, j] = sqlReader[j].ToString();
                            _dataset[i, j] = Double.Parse(simp[i, j]);
                        }
                        i++;
                    }
                }
                db3.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sorry, something wrong in your query :( because " + ex);
            }
            for (baris = 0; baris < jumlahData; baris++)
            {
                int col = 0;
                for (kolom = 1; kolom < jumlahAtribut; kolom++)
                {
                    dataset[baris, col] = _dataset[baris, kolom];
                    col++;
                }
            }
            
        }
        void normalisasi_data()
        {            
            /*
             * Proses Normalisasi Data
             * Mencari nilai maksimum dan minimum tiap kolom
             * Melakukan proses normalisasi data yaitu (data sebenarnya - min)/(max-min)
             */
            //mencari nilai maksimum tiap kolom
            max = new double[dBiner + 1];
            for (baris = 0; baris < dBiner + 1; baris++)
            {
                max[baris] = -1;
                for (kolom = 0; kolom < jumlahData; kolom++)
                {
                    if (dataset[kolom, baris] > max[baris])
                    {
                        max[baris] = dataset[kolom, baris];
                    }
                }
            }
            //mencari nilai minimum tiap kolom
            min = new double[dBiner + 1];
            for (baris = 0; baris < dBiner + 1; baris++)
            {
                min[baris] = max[baris];
                for (kolom = 0; kolom < jumlahData; kolom++)
                {
                    if (dataset[kolom, baris] < min[baris])
                    {
                        min[baris] = dataset[kolom, baris];
                    }
                }
            }
            //menghitung normalisasi data
            dataset_normal = new double[jumlahData, dBiner + 1];
            for (baris = 0; baris < jumlahData; baris++)
            {
                for (kolom = 0; kolom < dBiner + 1; kolom++)
                {
                    dataset_normal[baris, kolom] = (dataset[baris, kolom] - min[kolom]) / (max[kolom] - min[kolom]);
                }
            }
        }
        void cross_validation()
        {
            /*
             Proses K-Fold Cross Validation
             * Mengacak urutan data
             * Mencari indeks awal dan akhir data uji
             * Menentukan Data Uji
             * Menentukan Data Latih
             */
            fold_dataset_latih = new double[k][,];
            fold_dataset_uji = new double[k][,];
            uji = new int[k];
            latih = new int[k];
            /*
             Proses Mengacak Urutan Data
             * Generate angka dari 1 sampai jumlahData
             * Mengacak urutan angka dengan algoritma Fisher-Yates
             * Memasukkan dataset_normal ke dalam dataset_acak sesuai dengan indeks teracak
             */
            dataset_acak = new double[jumlahData, dBiner + 1];
            int[] angka = new int[jumlahData];
            for (baris = 0; baris < jumlahData; baris++)
            {
                angka[baris] = baris;
            }
            Shuffle(angka);
            for (baris = 0; baris < jumlahData; baris++)
            {
                for (kolom = 0; kolom < jumlahData; kolom++)
                {
                    if (angka[baris] == kolom)
                    {
                        for (fitur = 0; fitur < dBiner + 1; fitur++)
                        {
                            dataset_acak[baris, fitur] = dataset_normal[kolom, fitur];
                        }
                    }
                }
            }
            //Proses Mencari Indeks Awal dan Akhir Data Uji
            int[][] indeks = new int[k][];
            for (baris = 0; baris < indeks.Length; baris++)
            {
                indeks[baris] = new int[2];
            }
            cari_indeks(indeks);
            //Proses Menentukan Data Uji dan Latih
            for (baris = 0; baris < k; baris++)
            {
                data_uji(baris, indeks);
                data_latih(baris, indeks);
            }
        }
        void cari_indeks(int[][] indeks)
        {
            //Proses Mencari Indeks Awal dan Akhir Data Uji
            int interval = jumlahData / k, b;
            for (b = 0; b < k; b++)
            {
                //nilai indeks awal
                indeks[b][0] = b * interval;
                //nilai indeks akhir
                indeks[b][1] = ((b + 1) * interval) - 1;
            }
            //nilai indeks akhir ditambahkan dengan hasil modulo jika jumlahData tidak habis dibagi K
            indeks[k - 1][1] += (jumlahData % k);
        }
        void data_uji(int fold, int[][] indeks)
        {
            /*Proses Menentukan Data Uji*/
            //meenghitung banyaknya jumlah data uji
            uji[fold] = indeks[fold][1] - indeks[fold][0] + 1;
            fold_dataset_uji[fold] = new double[uji[fold], dBiner + 1];
            //menginisialisasi nilai baris dataset_acak = indeks awal
            int _baris = indeks[fold][0], b;
            for (b = 0; b < uji[fold]; b++)
            {
                for (kolom = 1; kolom < dBiner + 1; kolom++)
                {
                    fold_dataset_uji[fold][b, kolom] = dataset_acak[_baris, kolom];
                }
                _baris++;
            }
        }
        void data_latih(int fold, int[][] indeks)
        {
            /*Proses Penentuan Data Latih*/
            //Menghitung jumlah data latih
            latih[fold] = jumlahData - (indeks[fold][1] - indeks[fold][0] + 1);
            fold_dataset_latih[fold] = new double[latih[fold], dBiner + 1];
            //variabel _baris mewakili nilai baris pada dataset_acak
            //variabel b mewakili nilai baris pada fold_dataset_latih
            int _baris = 0, b = 0;
            while (b < latih[fold])
            {
                //untuk memilih baris yang tidak berada pada range indeks awal dan akhir
                if (_baris < indeks[fold][0] || _baris > indeks[fold][1])
                {
                    for (kolom = 0; kolom < dBiner + 1; kolom++)
                    {
                        fold_dataset_latih[fold][b, kolom] = dataset_acak[_baris, kolom];
                    }
                    ++b;
                }
                ++_baris;
            }
        }
        void inisialisasi_partikel()
        {
            /*Proses Inisialisasi Partikel*/
            vMax = new double[dKontinu];
            count = new int[jumlahPartikel];
            for (baris = 0; baris < jumlahPartikel; baris++)
            {
                bool kondisi = true;
                do
                {
                    count[baris] = 0;
                    double rand_v = acak.NextDouble();
                    for (kolom = 0; kolom < dBiner + dKontinu; kolom++)
                    {
                        //untuk partikel dimensi kontinu
                        if (kolom < dKontinu)
                        {
                            partikel[baris, kolom] = batasBawah[kolom] + acak.NextDouble() * (batasAtas[kolom] - batasBawah[kolom]);
                            vMax[kolom] = rand_v * (batasAtas[kolom] - batasBawah[kolom]);
                        }
                        else
                        {
                            //untuk partikel dimensi diskrit
                            partikel[baris, kolom] = acak.Next(0, 2);
                                //partikel[baris, kolom] = 1;
                            if (partikel[baris, kolom] == 1)
                            {
                                count[baris] += 1;
                            }
                        }
                    }
                    //jika jumlah fitur terpilih kurang dari/ sama dengan 1 maka akan dilakukan inisialisasi fitur ulang
                    if (count[baris] <= 1)
                    {
                        kondisi = true;
                        kolom = dKontinu;
                    }
                    else
                    {
                        kondisi = false;
                    }
                } while (kondisi == true);
            }
            //menginisialisasi kecepatan partikel awal = 0
            velocity = new double[jumlahPartikel, dBiner + dKontinu];
        }
        double[,] pso_svr()
        {
            /*
             * Proses PSO-SVR
             * Proses K-Fold Cross Validation
             * Proses Inisialisasi Partikel
             * Proses Melatih SVR dan Menghitung Error
             * Proses Mencari pBest
             * Proses Mencari gBest
             * Proses Menghitung Bobot Inersia
             * Proses Memperbarui Kecepatan Partikel
             * Proses Memperbarui Posisi Partikel
             */
            fold_dataset_latih_seleksi = new double[k][,];
            fold_dataset_uji_seleksi = new double[k][,];
            error = new double[k];
            solusi = new double[maxIterasi + 1, dBiner + dKontinu + 1];
            //for (int row = 0; row < maxIterasi; row++)
            //{
            //    for (int col = 0; col < dBiner + dKontinu; col++)
            //    {
            //        solusi[row, col] = 100000000000000000;
            //    }
            //}
            pBest = new double[jumlahPartikel, dBiner + dKontinu + 1];
            gBest = new double[1, dBiner + dKontinu + 1];
            tetangga = new double[3, dBiner + dKontinu + 1];
            euclidian_partikel = new double[jumlahPartikel];
            lBest = new double[jumlahPartikel, dBiner + dKontinu + 1];
            partikel = new double[jumlahPartikel, dBiner + dKontinu + 1];
            cross_validation();
            inisialisasi_partikel();
            load_partikel(partikel, dataGridView8);
            for (iterasi = 0; iterasi < maxIterasi + 1; iterasi++)
            {
                this.Text = "PSO_SVR 1.0 | Iterasi Ke- "+iterasi.ToString() + " dari " + maxIterasi.ToString();
                for (i = 0; i < jumlahPartikel; i++)
                {
                    for (fold = 0; fold < k; fold++)
                    {
                        error[fold] = svr(partikel[i, 0], partikel[i, 1], partikel[i, 2], partikel[i, 3], partikel[i, 4], i, fold);
                    }
                    //Menghitung fitness partikel
                    partikel[i, dKontinu + dBiner] = error.Average();
                }
                //Jika iterasi = 0 maka pBest = partikel dan kecepatan awal bernilai 0
                if (iterasi == 0)
                {
                    pBest = partikel;
                    //inisialisasi lBest sebagai pembanding
                    //lBest = pBest;
                    for (int row = 0; row < jumlahPartikel; row++)
                    {
                        
                            lBest[row, dBiner+dKontinu] = 100000000000000;
                    }
                }
                else
                {
                    pBest = cari_pBest();
                }
                cari_lBest();
                //menghitung bobot inersia
                double _maxIterasi = System.Convert.ToDouble(maxIterasi);
                double _iterasi = System.Convert.ToDouble(iterasi);
                inertia = (maxInersia - minInersia) * ((_maxIterasi - _iterasi) / _maxIterasi) + minInersia;
                velocity = update_kecepatan();
                partikel = update_posisi();
                //cari solusi

                //for (kolom = 0; kolom < (dBiner + dKontinu + 1); kolom++)
                //{
                //    solusi[iterasi, kolom] = gBest[0, kolom];
                //}
                //mencari solusi
                //for (kolom = 0; kolom < (dBiner + dKontinu + 1); kolom++)
                //{
                solusi[iterasi, dBiner + dKontinu] = 100000000000000;
                //}
                
                for (int r_lBest = 0; r_lBest < jumlahPartikel; r_lBest++)
                {
                    if (solusi[iterasi, dBiner + dKontinu] > lBest[r_lBest, dBiner + dKontinu])
                    {
                        for (kolom = 0; kolom < (dBiner + dKontinu + 1); kolom++)
                        {
                            solusi[iterasi, kolom] = lBest[r_lBest, kolom];
                        }
                    }
                }
            }
            load_partikel(partikel, dataGridView10);
            return solusi;
        }
        double svr(double c, double epsilon, double sigma, double cLR, double lambda, int i, int j)
        {
            /*
             Proses Melatih SVR dan Menghitung Error
             * Proses Memilih Fitur
             * Proses Sequential Learning
             * Proses Menguji Model Regresi
             * Proses Menghitung Error
             */
            memilih_fitur(i, j, count[i], out fold_dataset_latih_seleksi, out fold_dataset_uji_seleksi);
            sequential_learning(c, epsilon, sigma, cLR, lambda, j, count[i], out alpha, out alpha_star);
            fx = new double[uji[j]];
            fx = uji_model_regresi(sigma, lambda, j, count[i]);
            double error_partikel = hitung_error(j, count[i]);
            return error_partikel;
        }
        void memilih_fitur(int i, int j, int count, out double[][,] fold_dataset_l_seleksi, out double[][,] fold_dataset_u_seleksi)
        {
            /*Proses Memilih Fitur*/
            fold_dataset_l_seleksi = new double[k][,];
            fold_dataset_u_seleksi = new double[k][,];
            fold_dataset_l_seleksi[j] = new double[latih[j], count + 1];
            fold_dataset_u_seleksi[j] = new double[uji[j], count + 1];
            int k_latih = 0, k_uji = 0;
            for (int l = dKontinu; l < (dKontinu + dBiner); l++)
            {
                //jika partikel bernilai 1, maka fitur tersebut dipilih
                if (partikel[i, l] == 1)
                {
                    kolom = l - dKontinu;
                    for (int m = 0; m < latih[j]; m++)
                    {
                        fold_dataset_l_seleksi[j][m, k_latih] = fold_dataset_latih[j][m, kolom];
                        fold_dataset_l_seleksi[j][m, count] = fold_dataset_latih[j][m, dBiner];
                    }
                    k_latih += 1;
                    for (int m = 0; m < uji[j]; m++)
                    {
                        fold_dataset_u_seleksi[j][m, k_uji] = fold_dataset_uji[j][m, kolom];
                        fold_dataset_u_seleksi[j][m, count] = fold_dataset_uji[j][m, dBiner];
                    }
                    k_uji += 1;
                }
            }
        }
        double[,] model_regresi(double sigma, double lambda, int j, int count)
        {
            /*Proses Membentuk Model Regresi*/
            double[][,] matriks_hitung = new double[latih[j]][,];
            double[,] matriks_X = new double[latih[j], latih[j]];
            double[,] matriks_R = new double[latih[j], latih[j]];
            for (baris = 0; baris < latih[j]; baris++)
            {
                matriks_hitung[baris] = new double[latih[j], count];
                for (kolom = 0; kolom < latih[j]; kolom++)
                {
                    for (fitur = 0; fitur < count; fitur++)
                    {
                        //Menghitung jarak data
                        matriks_hitung[baris][kolom, fitur] = Math.Pow((Math.Abs(fold_dataset_latih_seleksi[j][baris, fitur] - fold_dataset_latih_seleksi[j][kolom, fitur])), 2);
                        matriks_X[baris, kolom] += matriks_hitung[baris][kolom, fitur];
                    }
                    //Membentuk matriks kernel
                    matriks_R[baris, kolom] = Math.Exp(-matriks_X[baris, kolom] / (2 * Math.Pow(sigma, 2))) + Math.Pow(lambda, 2);
                }
            }
            return matriks_R;

        }
        double learning_rate(int j, double cLR)
        {
            /*Proses Menghitung Learning*/
            //Mencari diagonal matriks kernel
            double[] diagonal = new double[latih[j]];
            for (baris = 0; baris < latih[j]; baris++)
            {
                for (kolom = 0; kolom < latih[j]; kolom++)
                {
                    if (baris == kolom)
                    {
                        diagonal[baris] = matriks_regresi[baris, kolom];
                    }
                }
            }
            //Menghitung learning rate
            double _gamma = cLR / diagonal.Max();
            return _gamma;
        }
        void sequential_learning(double c, double epsilon, double sigma, double cLR, double lambda, int j, int count, out double[] al, out double[] al_star)
        {
            /*
             Proses Sequential Learning
             * Inisialisasi nilai alpha dan alpha star
             * Menghitung matriks regresi
             * Menghitung Error
             * Menghitung Delta Alpha Star
             * Menghitung Delta Alpha
             * Menghitung Alpha Star
             * Menghitung Alpha
             */
            matriks_regresi = new double[dBiner, dBiner];
            matriks_regresi = model_regresi(sigma, lambda, j, count);
            gamma = learning_rate(j, cLR);
            //double _epsilon = 0.00000001;
            int iter = 0, maxIterasiLatih = 1000;
            bool cond = true;
            al = new double[latih[j]];
            al_star = new double[latih[j]];
            double[,] x = new double[latih[j], latih[j]];
            double[] sum_alpha_r = new double[latih[j]];
            double[] e = new double[latih[j]];
            double[] delta_alpha = new double[latih[j]];
            double[] delta_alpha_star = new double[latih[j]];
            do
            {
                for (baris = 0; baris < latih[j]; baris++)
                {
                    for (kolom = 0; kolom < latih[j]; kolom++)
                    {
                        //menghitung penjumlahan dari (alpha star -alpha) * matriks regresi
                        x[baris, kolom] = (al_star[baris] - al[baris]) * matriks_regresi[baris, kolom];
                        sum_alpha_r[baris] += x[baris, kolom];
                    }

                    e[baris] = fold_dataset_latih_seleksi[j][baris, count] - sum_alpha_r[baris];
                    delta_alpha_star[baris] = Math.Min(Math.Max(gamma * (e[baris] - epsilon), -al_star[baris]), c - al_star[baris]);
                    delta_alpha[baris] = Math.Min(Math.Max(gamma * (-e[baris] - epsilon), -al[baris]), c - al[baris]);
                    al_star[baris] = al_star[baris] + delta_alpha_star[baris];
                    al[baris] = al[baris] + delta_alpha[baris];
                }
                //mencari nilai maksimal delta alpha star dan delta alpha
                double max_delta_alpha_star = delta_alpha_star.Max();
                double max_delta_alpha = delta_alpha.Max();
                iter += 1;
                //jika kedua nilai max <= epsilon, maka proses learning berhenti
                if (max_delta_alpha_star <= epsilon && max_delta_alpha <= epsilon)
                //if (max_delta_alpha_star <= _epsilon && max_delta_alpha <= _epsilon)
                {
                    cond = false;
                }
                else { cond = true; }
                //atau jika iterasi = jumlah max iterasi yang telah ditentukan, maka proses learning berhenti
                if (iter == maxIterasiLatih)
                {
                    break;
                }
            } while (cond == true);
        }
        double[] uji_model_regresi(double sigma, double lambda, int j, int count)
        {
            /*Proses Menguji Model Regresi*/
            double[][,] matriks_hitung = new double[latih[j]][,];
            double[,] matriks_X = new double[uji[j], latih[j]];
            double[,] matriks_R = new double[uji[j], latih[j]];
            double[,] fx1 = new double[uji[j], latih[j]];
            double[] fx = new double[uji[j]];
            for (baris = 0; baris < uji[j]; baris++)
            {
                matriks_hitung[baris] = new double[latih[j], count];
                for (kolom = 0; kolom < latih[j]; kolom++)
                {
                    for (fitur = 0; fitur < count; fitur++)
                    {
                        //menghitung jarak data uji dan data latih
                        matriks_hitung[baris][kolom, fitur] = Math.Pow((Math.Abs(fold_dataset_latih_seleksi[j][kolom, fitur] - fold_dataset_uji_seleksi[j][baris, fitur])), 2);
                        matriks_X[baris, kolom] += matriks_hitung[baris][kolom, fitur];
                    }
                    //membentuk matriks kernel data uji dan data latih
                    matriks_R[baris, kolom] = Math.Exp(-matriks_X[baris, kolom] / (2 * Math.Pow(sigma, 2))) + Math.Pow(lambda, 2);
                    //memprediksi nilai target
                    fx1[baris, kolom] = (alpha_star[kolom] - alpha[kolom]) * matriks_R[baris, kolom];
                    fx[baris] += fx1[baris, kolom];
                }
                //denormalisasi nilai target
                fx[baris] = ((fx[baris] * (max[dBiner] - min[dBiner])) + min[dBiner]);
            }
            return fx;
        }
        double hitung_error(int j, int count)
        {
            /*Proses Menghitung Error*/
            double[] dataset_uji_normal = new double[uji[j]];
            double[] mape = new double[uji[j]];
            double[] aktual_prediksi = new double[1];
            for (baris = 0; baris < uji[j]; baris++)
            {
                //menghitung MAPE
                dataset_uji_normal[baris] = (fold_dataset_uji_seleksi[j][baris, count] * (max[dBiner] - min[dBiner])) + min[dBiner];
                aktual_prediksi[0] = Math.Abs((dataset_uji_normal[baris] - fx[baris]) / dataset_uji_normal[baris]);
                mape[baris] += aktual_prediksi[0];
            }
            double _dBiner = System.Convert.ToDouble(dBiner);
            double _count = System.Convert.ToDouble(count);
            //menghitung error
            double err = (0.95 * mape.Average()) + (0.05 * (_count / _dBiner));
            return err;
        }
        double[,] cari_pBest()
        {
            /*Proses Mencari pBest*/
            for (baris = 0; baris < jumlahPartikel; baris++)
            {
                //jika fitness pBest > fitness partikel
                if (pBest[baris, dBiner + dKontinu] > partikel[baris, dBiner + dKontinu])
                {
                    for (kolom = 0; kolom < (dBiner + dKontinu + 1); kolom++)
                    {
                        //maka pBest digantikan oleh partikel saat ini
                        pBest[baris, kolom] = partikel[baris, kolom];
                    }
                }
            }
            return pBest;
        }
        void cari_lBest()
        {
            double selisih_jarak = 0, squared = 0;
            //menghitung jarak euclidian
            for (baris = 0; baris < jumlahPartikel; baris++)
            {
                for (kolom = 0; kolom < jumlahPartikel; kolom++)
                {
                    for (int dimensi = 0; dimensi < (dBiner + dKontinu + 1); dimensi++)
                    {
                        squared = Math.Pow(pBest[baris, dimensi] - pBest[kolom, dimensi], 2);
                        selisih_jarak += squared;
                    }
                    euclidian_partikel[kolom] = Math.Sqrt(selisih_jarak);
                }
                //mencari neighborhood
                double min1 = 1000000000000000000;
                double min2 = 1000000000000000000;
                for (int y = 0; y < jumlahPartikel; y++)
                {
                    if (euclidian_partikel[y] < min1 && euclidian_partikel[y]!=0)
                    {
                        min1 = euclidian_partikel[y];
                        for (kolom = 0; kolom < (dBiner + dKontinu + 1); kolom++)
                        {
                            tetangga[0, kolom] = pBest[y, kolom];
                        }
                    }
                }
                for (int x = 0; x < jumlahPartikel; x++)
                {
                    if (euclidian_partikel[x] != min1 && euclidian_partikel[x] < min2)
                    {
                        min2 = euclidian_partikel[x];
                        for (kolom = 0; kolom < (dBiner + dKontinu + 1); kolom++)
                        {
                            tetangga[1, kolom] = pBest[x, kolom];
                        }
                    }
                }
                 
                for (kolom = 0; kolom < (dBiner + dKontinu + 1); kolom++)
                {
                    tetangga[2, kolom] = pBest[baris, kolom];
                }
                //mencari lBest
                for (int n_row = 0; n_row < 3; n_row++)
                {
                    if (lBest[baris, dBiner + dKontinu] > tetangga[n_row, dBiner + dKontinu])
                    {
                        for (kolom = 0; kolom < (dBiner + dKontinu + 1); kolom++)
                        {
                            lBest[baris, kolom] = tetangga[n_row, kolom];
                        }
                    }
                }
                //load_partikel(lBest, dataGridView8);
            }
        }
        void cari_gBest()
        {
            /*Proses Mencari gBest*/
            for (baris = 0; baris < jumlahPartikel; baris++)
            {
                //jika fitness gBest > fitness pBest
                if (gBest[0, dBiner + dKontinu] > pBest[baris, dBiner + dKontinu])
                {
                    for (kolom = 0; kolom < (dBiner + dKontinu + 1); kolom++)
                    {
                        //maka gBest akan digantikan oleh pBest saat ini
                        gBest[0, kolom] = pBest[baris, kolom];
                    }
                }
            }
        }
        double[,] update_kecepatan()
        {
            /*Proses Memperbarui Kecepatan Partikel*/
            double r1 = acak.NextDouble();
            double r2 = acak.NextDouble();
            for (baris = 0; baris < jumlahPartikel; baris++)
            {
                for (kolom = 0; kolom < dBiner + dKontinu; kolom++)
                {
                    //menghitung kecepatan partikel
                    //Console.WriteLine(lBest[baris,kolom]);
                    //velocity[baris, kolom] = inertia * velocity[baris, kolom] + c1 * r1 * (pBest[baris, kolom] - partikel[baris, kolom]) + c2 * r2 * (345.345346 - partikel[baris, kolom]);
                    velocity[baris, kolom] = inertia * velocity[baris, kolom] + c1 * r1 * (pBest[baris, kolom] - partikel[baris, kolom]) + c2 * r2 * (lBest[baris, kolom] - partikel[baris, kolom]);
                    //untuk partikel dimensi kontinu
                    if (kolom < dKontinu)
                    {
                        //jika kecepatan terbaru >= kecepatan maksimal
                        if (velocity[baris, kolom] >= vMax[kolom])
                        {
                            //maka kecepatan terbaru akan digantikan dengan kecepatan maksimal partikel
                            velocity[baris, kolom] = vMax[kolom];
                        }
                    }
                    //untuk partikel dimensi diskrit
                    else
                    {
                        //menormalisasi kecepatan terbaru dengan fungsi sigmoid
                        velocity[baris, kolom] = 1 / (1 + Math.Exp(velocity[baris, kolom]));
                    }
                }
            }
            return velocity;
        }
        double[,] update_posisi()
        {
            /*Proses Memperbarui Posisi Partikel*/
            for (baris = 0; baris < jumlahPartikel; baris++)
            {
                bool kondisi = true;
                do
                {
                    count[baris] = 0;
                    for (kolom = 0; kolom < dBiner + dKontinu; kolom++)
                    {
                        //untuk partikel dimensi kontinu
                        if (kolom < dKontinu)
                        {
                            partikel[baris, kolom] = partikel[baris, kolom] + velocity[baris, kolom];
                            //jika posisi partikel > batas atas ruang pencarian
                            if (partikel[baris, kolom] > batasAtas[kolom])
                            {
                                //maka posisi partikel digantikan oleh batas atas ruang pencarian
                                partikel[baris, kolom] = batasAtas[kolom];
                            }
                            //jika posisi partikel < batas atas ruang pencarian
                            else if (partikel[baris, kolom] < batasBawah[kolom])
                            {
                                //maka posisi partikel digantikan oleh batas bawah ruang pencarian
                                partikel[baris, kolom] = batasBawah[kolom];
                            }
                        }
                        //untuk partikel dimensi diskrit
                        else
                        {
                            double pos_acak = acak.NextDouble();
                            //jika nilai acak < kecepatan partikel
                            if (pos_acak < velocity[baris, kolom])
                            {
                                //maka partikel bernilai 1
                                partikel[baris, kolom] = 1;
                                count[baris] += 1;
                            }
                            //jika nilai acak >= kecepatan partikel
                            else if (pos_acak >= velocity[baris, kolom])
                            {
                                //maka partikel bernilai 0
                                partikel[baris, kolom] = 0;
                            }
                        }
                    }
                    //mengecek apakah jumlah fitur terpilih <=1
                    if (count[baris] <= 1)
                    {
                        //jika ya, maka dilakukan pembaruan posisi kembali
                        kondisi = true;
                        kolom = dKontinu;
                    }
                    else
                    {
                        kondisi = false;
                    }
                } while (kondisi == true);
            }
            return partikel;
        }
        void load_partikel(double [,] _partikel, DataGridView _datagrid)
        {
            Hasil[] arrHasil = new Hasil[jumlahPartikel];
            for (int baris = 0; baris < jumlahPartikel; baris++)
            {
                arrHasil[baris] = new Hasil();
                arrHasil[baris].Iterasi = baris;
                arrHasil[baris].C = _partikel[baris, 0];
                arrHasil[baris].Epsilon = _partikel[baris, 1];
                arrHasil[baris].Sigma = _partikel[baris, 2];
                arrHasil[baris].cLR = _partikel[baris, 3];
                arrHasil[baris].Lambda = _partikel[baris, 4];
                arrHasil[baris].F1 = _partikel[baris, 5];
                arrHasil[baris].F2 = _partikel[baris, 6];
                arrHasil[baris].F3 = _partikel[baris, 7];
                arrHasil[baris].F4 = _partikel[baris, 8];
                arrHasil[baris].F5 = _partikel[baris, 9];
                arrHasil[baris].F6 = _partikel[baris, 10];
                arrHasil[baris].F7 = _partikel[baris, 11];
                arrHasil[baris].Cost = _partikel[baris, 12];

            }
            _datagrid.DataSource = arrHasil;
        }
        void load_tabel(double[,] _solusi, DataGridView _datagrid)
        {
            Hasil[] arrHasil = new Hasil[maxIterasi + 1];
            for (int baris = 0; baris < maxIterasi + 1; baris++)
            {
                arrHasil[baris] = new Hasil();
                arrHasil[baris].Iterasi = baris;
                arrHasil[baris].C = _solusi[baris, 0];
                arrHasil[baris].Epsilon = _solusi[baris, 1];
                arrHasil[baris].Sigma = _solusi[baris, 2];
                arrHasil[baris].cLR = _solusi[baris, 3];
                arrHasil[baris].Lambda = _solusi[baris, 4];
                arrHasil[baris].F1 = _solusi[baris, 5];
                arrHasil[baris].F2 = _solusi[baris, 6];
                arrHasil[baris].F3 = _solusi[baris, 7];
                arrHasil[baris].F4 = _solusi[baris, 8];
                arrHasil[baris].F5 = _solusi[baris, 9];
                arrHasil[baris].F6 = _solusi[baris, 10];
                arrHasil[baris].F7 = _solusi[baris, 11];
                arrHasil[baris].Cost = _solusi[baris, 12];

            }
            _datagrid.DataSource = arrHasil;
            for (kolom = 0; kolom < dBiner + 2; kolom++)
            {
                _datagrid.Columns[kolom].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }
        void load_dataset(double[,] _data)
        {
            headerData[] arrHeaderData = new headerData[jumlahData];
            for (baris = 0; baris < jumlahData; baris++)
            {
                arrHeaderData[baris] = new headerData();
                arrHeaderData[baris].No = baris+1;
                arrHeaderData[baris].TeamExp = _data[baris, 0];
                arrHeaderData[baris].ManagerExp = _data[baris, 1];
                arrHeaderData[baris].Transaction = _data[baris, 2];
                arrHeaderData[baris].Entities = _data[baris, 3];
                arrHeaderData[baris].PointAdjust = _data[baris, 4];
                arrHeaderData[baris].Envergure = _data[baris, 5];
                arrHeaderData[baris].PointNonAdjust = _data[baris, 6];
                arrHeaderData[baris].Effort = _data[baris, 7];
            }
            dataGridView1.DataSource = arrHeaderData;
            for (kolom = 0; kolom < dBiner+2; kolom++)
            {
                dataGridView1.Columns[kolom].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }
        private void reset_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
            textBox2.Clear();
            textBox3.Clear();
            textBox4.Clear();
            textBox5.Clear();
            textBox6.Clear();
            textBox7.Clear();
            textBox8.Clear();
            textBox9.Clear();
            textBox10.Clear();
            textBox11.Clear();
            textBox12.Clear();
            textBox13.Clear();
            textBox14.Clear();
            textBox15.Clear();
            textBox16.Clear();
            textBox17.Clear();
        }
        void load_dimensi()
        {
            textBox51.Text = "10";
            textBox50.Text = "5";
            textBox49.Text = "1";
            textBox48.Text = "1";
            textBox47.Text = "0,8";
            textBox46.Text = "0,4";
            textBox45.Text = "3";
            textBox44.Text = "0,1";//c
            textBox37.Text = "0,001";//epsilon
            textBox41.Text = "0,1";//sigma
            textBox40.Text = "0,01";//cLR
            textBox35.Text = "0,01";//lmbda
            textBox43.Text = "1500";//c
            textBox42.Text = "0,09";//epsilon
            textBox38.Text = "4";//sigma
            textBox39.Text = "1,75";//cLR
            textBox36.Text = "3";//lambda

        }
        void load_fold()
        {
            textBox34.Text = textBox51.Text;
            textBox33.Text = textBox50.Text;
            textBox32.Text = textBox49.Text;
            textBox31.Text = textBox48.Text;
            textBox30.Text = textBox47.Text;
            textBox29.Text = textBox46.Text;
            textBox28.Text = "10";
            textBox27.Text = textBox44.Text;//
            textBox20.Text = textBox37.Text;
            textBox24.Text = textBox41.Text;
            textBox23.Text = textBox40.Text;
            textBox18.Text = textBox35.Text;
            textBox26.Text = textBox43.Text;
            textBox25.Text = textBox42.Text;
            textBox21.Text = textBox38.Text;
            textBox22.Text = textBox39.Text;
            textBox19.Text = textBox36.Text;
        }
        void load_inersia()
        {
            textBox68.Text = textBox51.Text;
            textBox67.Text = textBox50.Text;
            textBox66.Text = textBox49.Text;
            textBox65.Text = textBox48.Text;
            textBox64.Text = "0,6";
            textBox63.Text = "0,2";
            textBox62.Text = textBox28.Text;
            textBox61.Text = textBox44.Text;//
            textBox54.Text = textBox37.Text;
            textBox58.Text = textBox41.Text;
            textBox57.Text = textBox40.Text;
            textBox52.Text = textBox35.Text;
            textBox60.Text = textBox43.Text;
            textBox59.Text = textBox42.Text;
            textBox55.Text = textBox38.Text;
            textBox56.Text = textBox39.Text;
            textBox53.Text = textBox36.Text;
        }
        void load_aksel()
        {
            textBox85.Text = textBox51.Text;
            textBox84.Text = textBox50.Text;
            textBox83.Text = "1";
            textBox82.Text = "1,5";
            textBox81.Text = textBox64.Text;//wMax
            textBox80.Text = textBox63.Text;//wMin
            textBox79.Text = textBox28.Text;//fold
            textBox78.Text = textBox44.Text;//dimensi
            textBox71.Text = textBox37.Text;
            textBox75.Text = textBox41.Text;
            textBox74.Text = textBox40.Text;
            textBox69.Text = textBox35.Text;
            textBox77.Text = textBox43.Text;
            textBox76.Text = textBox42.Text;
            textBox72.Text = textBox38.Text;
            textBox73.Text = textBox39.Text;
            textBox70.Text = textBox36.Text;
        }
        void load_jPartikel()
        {
            textBox102.Text = "15";//partikel
            textBox101.Text = textBox50.Text;//maxIterasi
            textBox100.Text = textBox83.Text;//c1
            textBox99.Text = textBox82.Text;//c2
            textBox98.Text = textBox64.Text;//wMax
            textBox97.Text = textBox63.Text;//wMin
            textBox96.Text = textBox28.Text;//fold
            textBox95.Text = textBox44.Text;//dimensi
            textBox88.Text = textBox37.Text;
            textBox92.Text = textBox41.Text;
            textBox91.Text = textBox40.Text;
            textBox86.Text = textBox35.Text;
            textBox94.Text = textBox43.Text;
            textBox93.Text = textBox42.Text;
            textBox89.Text = textBox38.Text;
            textBox90.Text = textBox39.Text;
            textBox87.Text = textBox36.Text;
        }
        void load_mIterasi()
        {
            textBox119.Text = textBox102.Text;
            textBox118.Text = "40";
            textBox117.Text = textBox83.Text;//c1
            textBox116.Text = textBox82.Text;//c2
            textBox115.Text = textBox64.Text;//wMax
            textBox114.Text = textBox63.Text;//wMin
            textBox113.Text = textBox28.Text;//fold
            textBox112.Text = textBox44.Text;//dimensi
            textBox105.Text = textBox37.Text;
            textBox109.Text = textBox41.Text;
            textBox108.Text = textBox40.Text;
            textBox103.Text = textBox35.Text;
            textBox111.Text = textBox43.Text;
            textBox110.Text = textBox42.Text;
            textBox106.Text = textBox38.Text;
            textBox107.Text = textBox39.Text;
            textBox104.Text = textBox36.Text;
            textBox19.Text = textBox36.Text;
        }
        private void dimensi_Click(object sender, EventArgs e)
        {
            var watch = Stopwatch.StartNew();
            jumlahPartikel = int.Parse(textBox51.Text);
            maxIterasi = int.Parse(textBox50.Text);
            c1 = double.Parse(textBox49.Text);
            c2 = double.Parse(textBox48.Text);
            maxInersia = double.Parse(textBox47.Text);
            minInersia = double.Parse(textBox46.Text);
            k = int.Parse(textBox45.Text);
            batasAtas = new double[dKontinu];
            batasBawah = new double[dKontinu];
            batasBawah[0] = double.Parse(textBox44.Text);
            batasBawah[1] = double.Parse(textBox37.Text);
            batasBawah[2] = double.Parse(textBox41.Text);
            batasBawah[3] = double.Parse(textBox40.Text);
            batasBawah[4] = double.Parse(textBox35.Text);
            batasAtas[0] = double.Parse(textBox43.Text);
            batasAtas[1] = double.Parse(textBox42.Text);
            batasAtas[2] = double.Parse(textBox38.Text);
            batasAtas[3] = double.Parse(textBox39.Text);
            batasAtas[4] = double.Parse(textBox36.Text);
            normalisasi_data();
            solusi = pso_svr();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            this.Text = "PSO_SVR 1.0 | Iterasi Ke- " + (iterasi - 1).ToString() + " dari " + maxIterasi.ToString() + " | Waktu Komputasi: " + elapsedMs.ToString() + " ms";
            load_tabel(solusi, dataGridView4);
        }
        private void button_fold_Click(object sender, EventArgs e)
        {
            var watch = Stopwatch.StartNew();
            jumlahPartikel = int.Parse(textBox34.Text);
            maxIterasi = int.Parse(textBox33.Text);
            c1 = double.Parse(textBox32.Text);
            c2 = double.Parse(textBox31.Text);
            maxInersia = double.Parse(textBox30.Text);
            minInersia = double.Parse(textBox29.Text);
            k = int.Parse(textBox28.Text);
            batasAtas = new double[dKontinu];
            batasBawah = new double[dKontinu];
            batasBawah[0] = double.Parse(textBox27.Text);
            batasBawah[1] = double.Parse(textBox20.Text);
            batasBawah[2] = double.Parse(textBox24.Text);
            batasBawah[3] = double.Parse(textBox23.Text);
            batasBawah[4] = double.Parse(textBox18.Text);
            batasAtas[0] = double.Parse(textBox26.Text);
            batasAtas[1] = double.Parse(textBox25.Text);
            batasAtas[2] = double.Parse(textBox21.Text);
            batasAtas[3] = double.Parse(textBox22.Text);
            batasAtas[4] = double.Parse(textBox19.Text);
            normalisasi_data();
            solusi = pso_svr();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            this.Text = "PSO_SVR 1.0 | Iterasi Ke- " + (iterasi - 1).ToString() + " dari " + maxIterasi.ToString() + " | Waktu Komputasi: " + elapsedMs.ToString() + " ms";
            load_tabel(solusi, dataGridView3);
        }
        private void inersia_Click(object sender, EventArgs e)
        {
            var watch = Stopwatch.StartNew();
            jumlahPartikel = int.Parse(textBox68.Text);
            maxIterasi = int.Parse(textBox67.Text);
            c1 = double.Parse(textBox66.Text);
            c2 = double.Parse(textBox65.Text);
            maxInersia = double.Parse(textBox64.Text);
            minInersia = double.Parse(textBox63.Text);
            k = int.Parse(textBox62.Text);
            batasAtas = new double[dKontinu];
            batasBawah = new double[dKontinu];
            batasBawah[0] = double.Parse(textBox61.Text);
            batasBawah[1] = double.Parse(textBox54.Text);
            batasBawah[2] = double.Parse(textBox58.Text);
            batasBawah[3] = double.Parse(textBox57.Text);
            batasBawah[4] = double.Parse(textBox52.Text);
            batasAtas[0] = double.Parse(textBox60.Text);
            batasAtas[1] = double.Parse(textBox59.Text);
            batasAtas[2] = double.Parse(textBox55.Text);
            batasAtas[3] = double.Parse(textBox56.Text);
            batasAtas[4] = double.Parse(textBox53.Text);
            normalisasi_data();
            solusi = pso_svr();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            this.Text = "PSO_SVR 1.0 | Iterasi Ke- " + (iterasi - 1).ToString() + " dari " + maxIterasi.ToString() + " | Waktu Komputasi: " + elapsedMs.ToString() + " ms";
            load_tabel(solusi, dataGridView5);
        }
        private void aksel_Click(object sender, EventArgs e)
        {
            var watch = Stopwatch.StartNew();
            jumlahPartikel = int.Parse(textBox85.Text);
            maxIterasi = int.Parse(textBox84.Text);
            c1 = double.Parse(textBox83.Text);
            c2 = double.Parse(textBox82.Text);
            maxInersia = double.Parse(textBox81.Text);
            minInersia = double.Parse(textBox80.Text);
            k = int.Parse(textBox79.Text);
            batasAtas = new double[dKontinu];
            batasBawah = new double[dKontinu];
            batasBawah[0] = double.Parse(textBox78.Text);
            batasBawah[1] = double.Parse(textBox71.Text);
            batasBawah[2] = double.Parse(textBox75.Text);
            batasBawah[3] = double.Parse(textBox74.Text);
            batasBawah[4] = double.Parse(textBox69.Text);
            batasAtas[0] = double.Parse(textBox77.Text);
            batasAtas[1] = double.Parse(textBox76.Text);
            batasAtas[2] = double.Parse(textBox72.Text);
            batasAtas[3] = double.Parse(textBox73.Text);
            batasAtas[4] = double.Parse(textBox70.Text);
            normalisasi_data();
            solusi = pso_svr();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            this.Text = "PSO_SVR 1.0 | Iterasi Ke- " + (iterasi - 1).ToString() + " dari " + maxIterasi.ToString() + " | Waktu Komputasi: " + elapsedMs.ToString() + " ms";
            load_tabel(solusi, dataGridView6);
        }
        private void jPartikel_Click(object sender, EventArgs e)
        {
            var watch = Stopwatch.StartNew();
            jumlahPartikel = int.Parse(textBox102.Text);
            maxIterasi = int.Parse(textBox101.Text);
            c1 = double.Parse(textBox100.Text);
            c2 = double.Parse(textBox99.Text);
            maxInersia = double.Parse(textBox98.Text);
            minInersia = double.Parse(textBox97.Text);
            k = int.Parse(textBox96.Text);
            batasAtas = new double[dKontinu];
            batasBawah = new double[dKontinu];
            batasBawah[0] = double.Parse(textBox95.Text);
            batasBawah[1] = double.Parse(textBox88.Text);
            batasBawah[2] = double.Parse(textBox92.Text);
            batasBawah[3] = double.Parse(textBox91.Text);
            batasBawah[4] = double.Parse(textBox86.Text);
            batasAtas[0] = double.Parse(textBox94.Text);
            batasAtas[1] = double.Parse(textBox93.Text);
            batasAtas[2] = double.Parse(textBox89.Text);
            batasAtas[3] = double.Parse(textBox90.Text);
            batasAtas[4] = double.Parse(textBox87.Text);
            normalisasi_data();
            solusi = pso_svr();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            this.Text = "PSO_SVR 1.0 | Iterasi Ke- " + (iterasi - 1).ToString() + " dari " + maxIterasi.ToString() + " | Waktu Komputasi: " + elapsedMs.ToString() + " ms";
            load_tabel(solusi, dataGridView7);
        }
        private void mIterasi_Click(object sender, EventArgs e)
        {
            var watch = Stopwatch.StartNew();
            jumlahPartikel = int.Parse(textBox119.Text);
            maxIterasi = int.Parse(textBox118.Text);
            c1 = double.Parse(textBox117.Text);
            c2 = double.Parse(textBox116.Text);
            maxInersia = double.Parse(textBox115.Text);
            minInersia = double.Parse(textBox114.Text);
            k = int.Parse(textBox113.Text);
            batasAtas = new double[dKontinu];
            batasBawah = new double[dKontinu];
            batasBawah[0] = double.Parse(textBox112.Text);
            batasBawah[1] = double.Parse(textBox105.Text);
            batasBawah[2] = double.Parse(textBox109.Text);
            batasBawah[3] = double.Parse(textBox108.Text);
            batasBawah[4] = double.Parse(textBox103.Text);
            batasAtas[0] = double.Parse(textBox111.Text);
            batasAtas[1] = double.Parse(textBox110.Text);
            batasAtas[2] = double.Parse(textBox106.Text);
            batasAtas[3] = double.Parse(textBox107.Text);
            batasAtas[4] = double.Parse(textBox104.Text);
            normalisasi_data();
            solusi = pso_svr();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            this.Text = "PSO_SVR 1.0 | Iterasi Ke- " + (iterasi - 1).ToString() + " dari " + maxIterasi.ToString() + " | Waktu Komputasi: " + elapsedMs.ToString() + " ms";
            load_tabel(solusi, dataGridView9);
        }
        //private void insert_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        string Query = "insert into data_see.see_(TeamExp,ManagerExp,Transaction,Entities,PointAdjust,Envergure,PointNonAdjust,Effort) values('" + textBox120.Text + "','" + textBox121.Text + "','" + textBox122.Text + "','" + textBox123.Text + "','" + textBox124.Text + "','" + textBox125.Text + "','" + textBox126.Text + "','" + textBox127.Text + "');";
        //        MySqlConnection MyConn2 = new MySqlConnection(connectionSQL);
        //        MySqlCommand MyCommand2 = new MySqlCommand(Query, MyConn2);
        //        MySqlDataReader MyReader2;
        //        MyConn2.Open();
        //        MyReader2 = MyCommand2.ExecuteReader();
        //        MessageBox.Show("Data telah disimpan");
        //        while (MyReader2.Read())
        //        {
        //        }
        //        MyConn2.Close();
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }
        //}
        //private void update_Click(object sender, EventArgs e)
        //{
        //    try
        //    {

        //        string Query = "update data_see.see_ set TeamExp='" + textBox120.Text + "',ManagerExp='" + textBox121.Text + "',Transaction='" + textBox122.Text + "',Entities='" + textBox123.Text + "',PointAdjust='" + textBox124.Text + "',Envergure='" + textBox125.Text + "',PointNonAdjust='" + textBox126.Text + "',Effort='" + textBox127.Text + "'where id='" + textBox128.Text + "';";
        //        MySqlConnection MyConn2 = new MySqlConnection(connectionSQL);
        //        MySqlCommand MyCommand2 = new MySqlCommand(Query, MyConn2);
        //        MySqlDataReader MyReader2;
        //        MyConn2.Open();
        //        MyReader2 = MyCommand2.ExecuteReader();
        //        MessageBox.Show("Data Proyek ke-" + textBox128.Text + " telah diperbarui");
        //        while (MyReader2.Read())
        //        {
        //        }
        //        MyConn2.Close();
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }
        //}
        //private void delete_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        string Query = "delete from data_see.see_ where id='" + textBox128.Text + "';";
        //        MySqlConnection MyConn2 = new MySqlConnection(connectionSQL);
        //        MySqlCommand MyCommand2 = new MySqlCommand(Query, MyConn2);
        //        MySqlDataReader MyReader2;
        //        MyConn2.Open();
        //        MyReader2 = MyCommand2.ExecuteReader();
        //        MessageBox.Show("Data Proyek ke-"+textBox128.Text+" telah dihapus");
        //        while (MyReader2.Read())
        //        {
        //        }
        //        MyConn2.Close();
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }
        //}
        //private void refresh_Click(object sender, EventArgs e)
        //{
        //    load_data();
        //    load_dataset(dataset);
        //    textBox120.Clear();
        //    textBox121.Clear();
        //    textBox122.Clear();
        //    textBox123.Clear();
        //    textBox124.Clear();
        //    textBox125.Clear();
        //    textBox126.Clear();
        //    textBox127.Clear();
        //    textBox128.Clear();
        //}

     }
}
