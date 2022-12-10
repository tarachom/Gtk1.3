using Gtk;
using Npgsql;

namespace GtkTest
{
    class WindowStart : Window
    {
        NpgsqlDataSource? DataSource { get; set; }

        TreeStore? Store;
        TreeView? treeView;

        public WindowStart() : base("PostgreSQL + GTKSharp")
        {
            SetDefaultSize(1600, 900);
            SetPosition(WindowPosition.Center);

            DeleteEvent += delegate { Program.Quit(); };

            VBox vbox = new VBox();
            Add(vbox);

            #region Кнопки

            //Кнопки
            HBox hBoxButton = new HBox();
            vbox.PackStart(hBoxButton, false, false, 10);

            Button bConnect = new Button("Підключитись до PostgreSQL");
            bConnect.Clicked += OnConnect;
            hBoxButton.PackStart(bConnect, false, false, 10);

            Button bFill = new Button("Заповнити");
            bFill.Clicked += OnFill;
            hBoxButton.PackStart(bFill, false, false, 10);

            #endregion

            //Список
            HBox hboxTree = new HBox();
            vbox.PackStart(hboxTree, true, true, 0);

            AddColumn();

            ScrolledWindow scroll = new ScrolledWindow() { ShadowType = ShadowType.In };
            scroll.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            scroll.Add(treeView);

            hboxTree.PackStart(scroll, true, true, 10);

            ShowAll();
        }

        void AddColumn()
        {
            Store = new TreeStore(typeof(string), typeof(string));
            treeView = new TreeView(Store);

            treeView.AppendColumn(new TreeViewColumn("ID", new CellRendererText(), "text", 0) { Visible = false });
            treeView.AppendColumn(new TreeViewColumn("Дерево", new CellRendererText(), "text", 1));
        }

        void OnConnect(object? sender, EventArgs args)
        {
            string Server = "localhost";
            string UserId = "postgres";
            string Password = "1";
            int Port = 5432;
            string Database = "test";

            string conString = $"Server={Server};Username={UserId};Password={Password};Port={Port};Database={Database};SSLMode=Prefer;";

            DataSource = NpgsqlDataSource.Create(conString);

            OnFill(this, new EventArgs());
        }

        void OnFill(object? sender, EventArgs args)
        {
            if (DataSource != null)
            {
                Store!.Clear();

                NpgsqlCommand command = DataSource.CreateCommand(
                    @"
WITH RECURSIVE r AS 
(
    -- Перший запит в рекусії
    SELECT 
        id, 
        name, 
        parentid, 
        1 AS level 
    FROM 
        tab3
    WHERE 
        parentid = 0

    UNION ALL

    -- Всі наступні запити
    SELECT 
        tab3.id, 
        tab3.name, 
        tab3.parentid, 
        r.level + 1 AS level
    FROM tab3
        JOIN r ON tab3.parentid = r.id
)
SELECT 
    id, 
    name, 
    parentid, 
    level FROM r
ORDER BY level ASC
");

                NpgsqlDataReader reader = command.ExecuteReader();

                TreeIter rootIter = Store.AppendValues("0", " Номенклатура ");
                Dictionary<int, TreeIter> NodeDictionary = new Dictionary<int, TreeIter>();

                while (reader.Read())
                {
                    int id = (int)reader["id"];
                    string name = reader["name"].ToString() ?? "";
                    int parentid = (int)reader["parentid"];
                    int level = (int)reader["level"];

                    if (level == 1)
                    {
                        TreeIter Iter = Store.AppendValues(rootIter, id.ToString(), name);
                        NodeDictionary.Add(id, Iter);
                    }
                    else
                    {
                        TreeIter parentIter = NodeDictionary[parentid];

                        TreeIter Iter = Store.AppendValues(parentIter, id.ToString(), name);
                        NodeDictionary.Add(id, Iter);
                    }
                }

                reader.Close();

                treeView!.ExpandAll();
            }
        }

    }
}
