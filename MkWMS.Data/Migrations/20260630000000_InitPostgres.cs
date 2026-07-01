using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MkWMS.Data.Migrations
{
    public partial class InitPostgres : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(name: "documentnumberseq");

            migrationBuilder.CreateTable(
                name: "Контрагенты",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Наименование = table.Column<string>(maxLength: 200, nullable: false),
                    ИНН = table.Column<string>(maxLength: 20, nullable: true),
                    КПП = table.Column<string>(maxLength: 20, nullable: true),
                    Адрес = table.Column<string>(maxLength: 500, nullable: true),
                    ЯвляетсяПоставщиком = table.Column<bool>(nullable: false),
                    ЯвляетсяПокупателем = table.Column<bool>(nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Контрагенты", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Подразделения",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Наименование = table.Column<string>(maxLength: 200, nullable: false),
                    СкладId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Подразделения", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Роли",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Наименование = table.Column<string>(maxLength: 100, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Роли", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Склады",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Наименование = table.Column<string>(maxLength: 200, nullable: false),
                    Адрес = table.Column<string>(maxLength: 500, nullable: true),
                    Активен = table.Column<bool>(nullable: false, defaultValue: true)
                },
                constraints: table => table.PrimaryKey("PK_Склады", x => x.Id));

            migrationBuilder.CreateTable(
                name: "ТипыДокументов",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Наименование = table.Column<string>(maxLength: 200, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_ТипыДокументов", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Товары",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Наименование = table.Column<string>(maxLength: 200, nullable: false),
                    Артикул = table.Column<string>(maxLength: 100, nullable: true),
                    Штрихкод = table.Column<string>(maxLength: 50, nullable: true),
                    ЕдИзм = table.Column<string>(maxLength: 20, nullable: true),
                    ИспользуетСерНомера = table.Column<bool>(nullable: false),
                    ИспользуетПартии = table.Column<bool>(nullable: false),
                    ДатаСоздания = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    ЗакупочнаяЦена = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                    РозничнаяЦена = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                    СтавкаНДС = table.Column<decimal>(precision: 5, scale: 2, nullable: false),
                    ПодлежитМаркировке = table.Column<bool>(nullable: false),
                    ВетеринарнаяСертификация = table.Column<bool>(nullable: false),
                    RadioTag = table.Column<string>(maxLength: 100, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_Товары", x => x.Id));

            migrationBuilder.CreateTable(
                name: "ЯчейкиХранения",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    СкладId = table.Column<int>(nullable: false),
                    Наименование = table.Column<string>(maxLength: 100, nullable: false),
                    РФИДМетка = table.Column<string>(maxLength: 100, nullable: true),
                    ТипЯчейки = table.Column<string>(maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ЯчейкиХранения", x => x.Id);
                    table.ForeignKey(name: "FK_ЯчейкиХранения_Склады_СкладId", column: x => x.СкладId, principalTable: "Склады", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Пользователи",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Логин = table.Column<string>(maxLength: 100, nullable: false),
                    ХешПароля = table.Column<string>(nullable: false),
                    ПолноеИмя = table.Column<string>(maxLength: 200, nullable: true),
                    Активен = table.Column<bool>(nullable: false, defaultValue: true),
                    ДатаСоздания = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    СкладId = table.Column<int>(nullable: true),
                    ТребуетСменыПароля = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Пользователи", x => x.Id);
                    table.ForeignKey(name: "FK_Пользователи_Склады_СкладId", column: x => x.СкладId, principalTable: "Склады", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Партии",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ТоварId = table.Column<int>(nullable: false),
                    НомерПартии = table.Column<string>(maxLength: 100, nullable: false),
                    ДатаПроизводства = table.Column<DateOnly>(nullable: true),
                    СрокГодностиДо = table.Column<DateOnly>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Партии", x => x.Id);
                    table.ForeignKey(name: "FK_Партии_Товары_ТоварId", column: x => x.ТоварId, principalTable: "Товары", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "СерийныеНомера",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ТоварId = table.Column<int>(nullable: false),
                    Номер = table.Column<string>(maxLength: 100, nullable: false),
                    Статус = table.Column<string>(maxLength: 50, nullable: true),
                    РФИДМетка = table.Column<string>(maxLength: 100, nullable: true),
                    КодМаркировки = table.Column<string>(maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_СерийныеНомера", x => x.Id);
                    table.ForeignKey(name: "FK_СерийныеНомера_Товары_ТоварId", column: x => x.ТоварId, principalTable: "Товары", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ПользователиРоли",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ПользовательId = table.Column<int>(nullable: false),
                    РольId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ПользователиРоли", x => x.Id);
                    table.ForeignKey(name: "FK_ПользователиРоли_Пользователи_ПользовательId", column: x => x.ПользовательId, principalTable: "Пользователи", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(name: "FK_ПользователиРоли_Роли_РольId", column: x => x.РольId, principalTable: "Роли", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ТокеныОбновления",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ПользовательId = table.Column<int>(nullable: false),
                    Токен = table.Column<string>(maxLength: 200, nullable: false),
                    ДатаИстечения = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    ДатаСоздания = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    ДатаОтзыва = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    ЗамененТокеном = table.Column<string>(maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ТокеныОбновления", x => x.Id);
                    table.ForeignKey(name: "FK_ТокеныОбновления_Пользователи_ПользовательId", column: x => x.ПользовательId, principalTable: "Пользователи", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Документы",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    НомерДокумента = table.Column<string>(maxLength: 50, nullable: false),
                    Статус = table.Column<string>(maxLength: 20, nullable: false),
                    Комментарий = table.Column<string>(maxLength: 1000, nullable: true),
                    ДатаСоздания = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    ТипДокументаId = table.Column<int>(nullable: false),
                    СкладId = table.Column<int>(nullable: false),
                    ПодразделениеId = table.Column<int>(nullable: true),
                    КреаторId = table.Column<int>(nullable: false),
                    КонтрагентId = table.Column<int>(nullable: true),
                    ВнешнийНомер = table.Column<string>(maxLength: 50, nullable: true),
                    ВнешняяДата = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Документы", x => x.Id);
                    table.ForeignKey(name: "FK_Документы_ТипыДокументов_ТипДокументаId", column: x => x.ТипДокументаId, principalTable: "ТипыДокументов", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_Документы_Склады_СкладId", column: x => x.СкладId, principalTable: "Склады", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_Документы_Контрагенты_КонтрагентId", column: x => x.КонтрагентId, principalTable: "Контрагенты", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(name: "FK_Документы_Пользователи_КреаторId", column: x => x.КреаторId, principalTable: "Пользователи", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "СтрокиДокументов",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ДокументId = table.Column<int>(nullable: false),
                    ТоварId = table.Column<int>(nullable: false),
                    ПартияId = table.Column<int>(nullable: true),
                    СерНомерId = table.Column<int>(nullable: true),
                    Количество = table.Column<decimal>(precision: 18, scale: 4, nullable: false),
                    Цена = table.Column<decimal>(precision: 18, scale: 2, nullable: true),
                    СуммаНДС = table.Column<decimal>(precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    ЯчейкаId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_СтрокиДокументов", x => x.Id);
                    table.ForeignKey(name: "FK_СтрокиДокументов_Документы_ДокументId", column: x => x.ДокументId, principalTable: "Документы", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(name: "FK_СтрокиДокументов_Товары_ТоварId", column: x => x.ТоварId, principalTable: "Товары", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_СтрокиДокументов_Партии_ПартияId", column: x => x.ПартияId, principalTable: "Партии", principalColumn: "Id");
                    table.ForeignKey(name: "FK_СтрокиДокументов_СерийныеНомера_СерНомерId", column: x => x.СерНомерId, principalTable: "СерийныеНомера", principalColumn: "Id");
                    table.ForeignKey(name: "FK_СтрокиДокументов_ЯчейкиХранения_ЯчейкаId", column: x => x.ЯчейкаId, principalTable: "ЯчейкиХранения", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Остатки",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ТоварId = table.Column<int>(nullable: false),
                    СкладId = table.Column<int>(nullable: false),
                    ПартияId = table.Column<int>(nullable: true),
                    Количество = table.Column<decimal>(precision: 18, scale: 4, nullable: false),
                    ЯчейкаId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Остатки", x => x.Id);
                    table.ForeignKey(name: "FK_Остатки_Товары_ТоварId", column: x => x.ТоварId, principalTable: "Товары", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_Остатки_Склады_СкладId", column: x => x.СкладId, principalTable: "Склады", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_Остатки_ЯчейкиХранения_ЯчейкаId", column: x => x.ЯчейкаId, principalTable: "ЯчейкиХранения", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ДвиженияТоваров",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ДокументId = table.Column<int>(nullable: false),
                    ТоварId = table.Column<int>(nullable: false),
                    СкладId = table.Column<int>(nullable: false),
                    ПартияId = table.Column<int>(nullable: true),
                    СерНомерId = table.Column<int>(nullable: true),
                    ЯчейкаId = table.Column<int>(nullable: true),
                    ИзменениеКоличества = table.Column<decimal>(precision: 18, scale: 4, nullable: false),
                    ДатаДвижения = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    Цена = table.Column<decimal>(precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ДвиженияТоваров", x => x.Id);
                    table.ForeignKey(name: "FK_ДвиженияТоваров_Документы_ДокументId", column: x => x.ДокументId, principalTable: "Документы", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(name: "FK_ДвиженияТоваров_Товары_ТоварId", column: x => x.ТоварId, principalTable: "Товары", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_ДвиженияТоваров_ЯчейкиХранения_ЯчейкаId", column: x => x.ЯчейкаId, principalTable: "ЯчейкиХранения", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ЖурналДействий",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ПользовательId = table.Column<int>(nullable: false),
                    Действие = table.Column<string>(maxLength: 200, nullable: false),
                    ТипСущности = table.Column<string>(maxLength: 100, nullable: true),
                    ИдСущности = table.Column<string>(maxLength: 50, nullable: true),
                    Детали = table.Column<string>(nullable: true),
                    ДатаДействия = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ЖурналДействий", x => x.Id);
                    table.ForeignKey(name: "FK_ЖурналДействий_Пользователи_ПользовательId", column: x => x.ПользовательId, principalTable: "Пользователи", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "__EFMigrationsHistory",
                columns: table => new
                {
                    MigrationId = table.Column<string>(maxLength: 150, nullable: false),
                    ProductVersion = table.Column<string>(maxLength: 32, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK___EFMigrationsHistory", x => x.MigrationId));

            migrationBuilder.CreateIndex(name: "IX_ЯчейкиХранения_СкладId", table: "ЯчейкиХранения", column: "СкладId");
            migrationBuilder.CreateIndex(name: "IX_Пользователи_СкладId", table: "Пользователи", column: "СкладId");
            migrationBuilder.CreateIndex(name: "UX_Пользователи_Логин", table: "Пользователи", column: "Логин", unique: true);
            migrationBuilder.CreateIndex(name: "UX_Роли_Наименование", table: "Роли", column: "Наименование", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Партии_ТоварId", table: "Партии", column: "ТоварId");
            migrationBuilder.CreateIndex(name: "IX_СерийныеНомера_ТоварId", table: "СерийныеНомера", column: "ТоварId");
            migrationBuilder.CreateIndex(name: "UX_Товары_Штрихкод", table: "Товары", column: "Штрихкод", unique: true, filter: "\"Штрихкод\" IS NOT NULL");
            migrationBuilder.CreateIndex(name: "IX_ПользователиРоли_ПользовательId", table: "ПользователиРоли", column: "ПользовательId");
            migrationBuilder.CreateIndex(name: "IX_ПользователиРоли_РольId", table: "ПользователиРоли", column: "РольId");
            migrationBuilder.CreateIndex(name: "UX_ТокеныОбновления_Токен", table: "ТокеныОбновления", column: "Токен", unique: true);
            migrationBuilder.CreateIndex(name: "IX_ТокеныОбновления_Пользователь", table: "ТокеныОбновления", column: "ПользовательId");
            migrationBuilder.CreateIndex(name: "IX_Документы_ТипДокументаId", table: "Документы", column: "ТипДокументаId");
            migrationBuilder.CreateIndex(name: "IX_Документы_СкладId", table: "Документы", column: "СкладId");
            migrationBuilder.CreateIndex(name: "IX_Документы_КонтрагентId", table: "Документы", column: "КонтрагентId");
            migrationBuilder.CreateIndex(name: "IX_Документы_КреаторId", table: "Документы", column: "КреаторId");
            migrationBuilder.CreateIndex(name: "IX_СтрокиДокументов_Документ", table: "СтрокиДокументов", column: "ДокументId");
            migrationBuilder.CreateIndex(name: "IX_СтрокиДокументов_Ячейка", table: "СтрокиДокументов", column: "ЯчейкаId");
            migrationBuilder.CreateIndex(name: "IX_Остатки_ТоварСклад", table: "Остатки", columns: new[] { "ТоварId", "СкладId" });
            migrationBuilder.CreateIndex(name: "IX_Остатки_ТоварСкладПартияЯчейка", table: "Остатки", columns: new[] { "ТоварId", "СкладId", "ПартияId", "ЯчейкаId" });
            migrationBuilder.CreateIndex(name: "IX_ДвиженияТоваров_ДокументId", table: "ДвиженияТоваров", column: "ДокументId");
            migrationBuilder.CreateIndex(name: "IX_ДвиженияТоваров_ТоварId", table: "ДвиженияТоваров", column: "ТоварId");
            migrationBuilder.CreateIndex(name: "IX_ЖурналДействий_ПользовательId", table: "ЖурналДействий", column: "ПользовательId");
            migrationBuilder.CreateIndex(name: "IX_Подразделения_СкладId", table: "Подразделения", column: "СкладId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ЖурналДействий");
            migrationBuilder.DropTable(name: "ДвиженияТоваров");
            migrationBuilder.DropTable(name: "Остатки");
            migrationBuilder.DropTable(name: "СтрокиДокументов");
            migrationBuilder.DropTable(name: "ТокеныОбновления");
            migrationBuilder.DropTable(name: "ПользователиРоли");
            migrationBuilder.DropTable(name: "Подразделения");
            migrationBuilder.DropTable(name: "Документы");
            migrationBuilder.DropTable(name: "СерийныеНомера");
            migrationBuilder.DropTable(name: "Партии");
            migrationBuilder.DropTable(name: "ЯчейкиХранения");
            migrationBuilder.DropTable(name: "Пользователи");
            migrationBuilder.DropTable(name: "Контрагенты");
            migrationBuilder.DropTable(name: "Роли");
            migrationBuilder.DropTable(name: "Склады");
            migrationBuilder.DropTable(name: "ТипыДокументов");
            migrationBuilder.DropTable(name: "Товары");
            migrationBuilder.DropTable(name: "__EFMigrationsHistory");
            migrationBuilder.DropSequence(name: "documentnumberseq");
        }
    }
}
