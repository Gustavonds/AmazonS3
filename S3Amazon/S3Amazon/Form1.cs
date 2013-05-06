using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace S3Amazon
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            UpdateExplorer();
        }


        /// <summary>
        /// Atualiza a árvore de arquivos do S3
        /// </summary>
        private void UpdateExplorer()
        {
            try
            {
                //Limpa a lista de nodos da árvore de arquivos
                explorer.Nodes.Clear();

                //Cria o cliente de S3 que irá realizar as ações no Cloud
                AmazonS3 s3Client = AWSClientFactory.CreateAmazonS3Client();

                //Recupera a lista de todos os buckets existentes
                ListBucketsResponse listBucket = s3Client.ListBuckets();
                foreach (S3Bucket bucket in listBucket.Buckets)
                {
                    //Cria nodo para representar o bucket
                    TreeNode nodeBucket = new TreeNode("Bucket - " + bucket.BucketName);
                    nodeBucket.Tag = bucket;
                    explorer.Nodes.Add(nodeBucket);

                    ListObjectsResponse objResponses = s3Client.ListObjects(new ListObjectsRequest().
                                                                            WithBucketName(bucket.BucketName));

                    TreeNode nodeAux;
                    foreach (S3Object s3Obj in objResponses.S3Objects)
                    {
                        nodeAux = new TreeNode(s3Obj.Key);
                        nodeAux.Tag = s3Obj;
                        nodeBucket.Nodes.Add(nodeAux);
                    }
                }

                explorer.ExpandAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Cria um novo bucket
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAddBucket_Click(object sender, EventArgs e)
        {
            try
            {
                // Cria o cliente S3
                AmazonS3 s3Client = AWSClientFactory.CreateAmazonS3Client();

                string newBucket = "";
                
                // Solicita o nome do novo bucket
                if (InputBox("Adicionar novo Bucket", "Nome do novo bucket", ref newBucket) == DialogResult.OK)
                {
                    // Envia uma requisição para a criação do novo bucket
                    PutBucketResponse bucketResponse = s3Client.PutBucket(new PutBucketRequest().
                                                                          WithBucketName(newBucket));

                    // Atualiza o explorer
                    UpdateExplorer();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public static DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }

        /// <summary>
        /// Remove um bucket selecionado
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRemoveBucket_Click(object sender, EventArgs e)
        {
            try
            {
                // Nodo selecionado 
                TreeNode node = explorer.SelectedNode;

                if (node != null)
                {
                    // Cria o cliente S3
                    AmazonS3 s3Client = AWSClientFactory.CreateAmazonS3Client();

                    // Caso o nodo tenha um pai, então é um objeto, caso contrário é um bucket
                    if (node.Parent == null)
                    {
                        // Para remover um bucket, é necessário remover todos seus objetos antes.
                        ListObjectsResponse listS3Objs = s3Client.ListObjects(new ListObjectsRequest()
                                                                       {
                                                                           BucketName = ((S3Bucket)node.Tag).BucketName
                                                                       });

                        foreach (S3Object s3Obj in listS3Objs.S3Objects)
                        {
                            
                            s3Client.DeleteObject(new DeleteObjectRequest()
                                                  {
                                                      BucketName = ((S3Bucket)node.Tag).BucketName,
                                                      Key = s3Obj.Key
                                                  });
                        }

                        // Remove o bucket selecionado
                        s3Client.DeleteBucket(new DeleteBucketRequest()
                                                {
                                                    BucketName = ((S3Bucket)node.Tag).BucketName
                                                });
                    }
                    else
                    {
                        MessageBox.Show("Seleciona um bucket a ser removido");
                    }

                    // Atualiza o explorer
                    UpdateExplorer();
                }
                else
                    MessageBox.Show("Selecione um bucket para ser removido");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        /// <summary>
        /// Remove uma pasta selecionada
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRemoveFolder_Click(object sender, EventArgs e)
        {
            try
            {
                // Nodo selecionado 
                TreeNode node = explorer.SelectedNode;

                if (node != null)
                {
                    // Cria o cliente S3
                    AmazonS3 s3Client = AWSClientFactory.CreateAmazonS3Client();

                    // Caso o nodo tenha um pai, então é um objeto, caso contrário é um bucket
                    if (node.Parent != null)
                    {
                        // Remove o objeto selecionado
                        s3Client.DeleteObject(new DeleteObjectRequest()
                                             {
                                                 BucketName = ((S3Bucket)node.Parent.Tag).BucketName,
                                                 Key = ((S3Object)node.Tag).Key
                                             });
                    }
                    else
                    {
                        MessageBox.Show("Seleciona um objeto a ser removido");
                    }

                    // Atualiza o explorer
                    UpdateExplorer();
                }
                else
                    MessageBox.Show("Selecione um bucket para ser removido");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        /// <summary>
        /// Cria uma nova pasta
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAddFolder_Click(object sender, EventArgs e)
        {
            try
            {
                // Nodo selecionado 
                TreeNode node = explorer.SelectedNode;

                if (node != null)
                {
                    // Cria o cliente S3
                    AmazonS3 s3Client = AWSClientFactory.CreateAmazonS3Client();

                    String folder = "";
                    if (InputBox("Adicionar nova Pasta", "Nome da nova pasta", ref folder) == DialogResult.OK)
                    {

                        // Caso o nodo não possui um pai, é um bucket, então será uma pasta na raiz do bucket
                        //caso contrário é necessário concatenar o caminho da pasta pai
                        if (node.Parent != null)
                        {
                            folder = ((S3Object)node.Tag).Key + folder;
                        }

                        s3Client.PutObject(new PutObjectRequest()
                                            {
                                                // Caso seja o nodo raiz ele será um bucket, caso contrário será um object
                                                BucketName = node.Tag is S3Bucket ? ((S3Bucket)node.Tag).BucketName : ((S3Object)node.Tag).BucketName,

                                                // O que define se um objeto será um arquivo ou pasta é a "/" no final da chave
                                                Key = folder.EndsWith("/") ? folder : folder + "/",

                                                ContentBody = " "
                                            });

                        // Atualiza explorer
                        UpdateExplorer();
                    }
                }
                        
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Realiza o upload dos arquivos
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUpload_Click(object sender, EventArgs e)
        {
            try
            {
                // Nodo selecionado 
                TreeNode node = explorer.SelectedNode;

                if (node != null)
                {
                    if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        // Recupera o nome do arquivo somente
                        String fileNameKey = fileDialog.FileName.Substring(fileDialog.FileName.LastIndexOf("\\") + 1);

                        // Cria o cliente S3
                        AmazonS3 s3Client = AWSClientFactory.CreateAmazonS3Client();

                        // Caso o nodo não possui um pai, é um bucket, então será um arquivo na raiz do bucket
                        //caso contrário é necessário concatenar o caminho da pasta pai
                        if (node.Parent != null)
                        {
                            fileNameKey = ((S3Object)node.Tag).Key + fileNameKey;
                        }

                        s3Client.PutObject(new PutObjectRequest()
                                          {
                                              // Caso seja o nodo raiz ele será um bucket, caso contrário será um object
                                              BucketName = node.Tag is S3Bucket ? ((S3Bucket)node.Tag).BucketName : ((S3Object)node.Tag).BucketName,

                                              Key = fileNameKey,

                                              FilePath = fileDialog.FileName
                                          });

                        // Atualiza explorer
                        UpdateExplorer();
                    }
                }
                else
                {
                    MessageBox.Show("Seleciona o bucket ou a pasta que você deseja carregar o arquivo");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Realiza o donwload dos arquivos
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDownload_Click(object sender, EventArgs e)
        {
            try
            {
                // Nodo selecionado 
                TreeNode node = explorer.SelectedNode;

                if (node != null)
                {
                    // Somente é permitido o download de um arquivo
                    if (node.Parent != null && !node.Text.EndsWith("//"))
                    {
                        if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            // Cria o cliente S3
                            AmazonS3 s3Client = AWSClientFactory.CreateAmazonS3Client();

                            // Requisita um stream do arquivo a ser baixado
                            S3Response s3Resp = s3Client.GetObject(new GetObjectRequest()
                                                                  {
                                                                      BucketName = node.Tag is S3Bucket ? ((S3Bucket)node.Tag).BucketName : ((S3Object)node.Tag).BucketName,
                                                                      Key = ((S3Object)node.Tag).Key
                                                                  });

                            // A partir de um stream retornado pelo S3Client deve ser
                            //realizada sua leitura e armazanado em um arquivo
                            using(FileStream fs = File.Create(fileDialog.FileName))
                            {
                                byte[] buffer = new byte[4096];
                                Stream str = s3Resp.ResponseStream;

                                int n = 1;

                                while (n != 0)
                                {
                                    n = str.Read(buffer, 0, 4096);

                                    if (n > 0)
                                        fs.Write(buffer, 0, n);
                                }

                            }
                            

                            // Atualiza explorer
                            UpdateExplorer();
                        }
                    }
                    else
                        MessageBox.Show("Somente é permitido o download de um arquivo");
                }
                else
                    MessageBox.Show("Seleciona o bucket ou a pasta que você deseja carregar o arquivo");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

    }
}
