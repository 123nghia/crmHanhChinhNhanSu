
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;

using VS.Human.Item;
using VS.Human.Rep;

namespace VS.Human.Business.Imp
{
    public class ExportFileBussiness : BaseBusiness, IExportFileBussiness
    {
        private readonly IDashboardBusinness _businness;
        public ExportFileBussiness(IUnitOfWork unitOfWork,
            IDashboardBusinness dashboardBusinness,
            IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {
            _businness = dashboardBusinness;
        }

        public async Task<string> ExportFileDashboard(OrderRequest requestExport)
        {

            var fromText = "";
            var toText = "";
            if (requestExport.From.HasValue)
            {
                fromText = requestExport.From.Value.ToString("dd-MM-yyyy");
            }

            if (requestExport.To.HasValue)
            {
                toText = requestExport.To.Value.ToString("dd-MM-yyyy");
            }
            var userName = requestExport.UserName;
            var dateGet = DateTime.Now;
            var fileName = dateGet.ToString("dd.MM.yy") + new Random(100).Next() + ".xlsx";
            var rootPath = "C:\\crmHuman\\wwwroot\\report";
            var pathFolder = Path.Combine(rootPath, userName);
            var exists = Directory.Exists(pathFolder);
            var pathfileName = userName + "\\" + fileName;
            if (!exists)
                Directory.CreateDirectory(pathFolder);
            var pathFile = Path.Combine(pathFolder, fileName);
            if (File.Exists(pathFile)) File.Delete(pathFile);

            using (var document = SpreadsheetDocument.Create(pathFile,
                 SpreadsheetDocumentType.Workbook))
            {

                var relationshipId = "overView";
                var workbookPart = document.AddWorkbookPart();
                var workbook = new Workbook();
                var sheets = new Sheets();
                var sheet1 = new Sheet
                {
                    Name = "overview",
                    SheetId = 1,
                    Id = relationshipId
                };
                var relationshipId2 = "Order-detail";
                var sheetOrderDetail = new Sheet
                {
                    Name = "Data",
                    SheetId = 2,
                    Id = relationshipId2
                };
                sheets.Append(sheet1);
                sheets.Append(sheetOrderDetail);
                workbook.Append(sheets);
                workbookPart.Workbook = workbook;
                var workSheetPart = workbookPart.AddNewPart<WorksheetPart>(relationshipId);
                var workSheet = new Worksheet();
                var sheetData = new SheetData();
                workSheet.Append(sheetData);
                workSheetPart.Worksheet = workSheet;

                //order detail

                var workSheetOrderPart = workbookPart.AddNewPart<WorksheetPart>(relationshipId2);
                var workSheetOrder = new Worksheet();
                var sheetDataOrder = new SheetData();
                workSheetOrder.Append(sheetDataOrder);
                workSheetOrderPart.Worksheet = workSheetOrder;


                document.PackageProperties.Creator = "Vietstargroup";
                document.PackageProperties.Created = DateTime.UtcNow;
                var allOrder = await _businness.GetALLOrderInfo(requestExport);
                var allRowStatus = await _businness.GetDashboardStatus(requestExport);
                double sumTotalCV = 0;
                double totalCVProcess = 0;
                double totalOnboard = 0;
                double cvDone = 0;
                double totalCVNew = 0;
                double totalDone = 0;
                foreach (var item in allOrder.Data)
                {
                    sumTotalCV++;
                    var iteminfo = item as dynamic;

                    if (iteminfo.Assignee < 1 || iteminfo.Assignee == null)
                    {
                        totalCVNew++;
                        continue;
                    }

                    if (iteminfo.Result < 1 || iteminfo.Result == null)
                    {
                        totalCVProcess++;
                        continue;

                    }
                    if (iteminfo.Result == 1)
                    {
                        totalOnboard++;
                        continue;
                    }

                    if (iteminfo.Result == 2)
                    {
                        cvDone++;

                        continue;
                    }
                }
                var row1 = new Row();
                row1.RowIndex = 1;
                row1.Append(
                        new Cell
                        {
                            DataType = CellValues.String,
                            CellValue = new CellValue("Ngày báo cáo")
                        },
                         new Cell
                         {
                             DataType = CellValues.String,
                             CellValue = new CellValue(DateTime.Now.ToString("dd.MM.yyyy"))
                         }

                );


                var rowform2 = new Row();
                rowform2.RowIndex = 2;

                rowform2.Append(
                    new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue("Từ ngày")
                    },
                     new Cell
                     {
                         DataType = CellValues.String,
                         CellValue = new CellValue(fromText)
                     },
                     new Cell
                     {
                         DataType = CellValues.String,
                         CellValue = new CellValue("Đến ngày")
                     },
                     new Cell
                     {
                         DataType = CellValues.String,
                         CellValue = new CellValue(toText)
                     }

                );

                var row2 = new Row();
                row2.RowIndex = 3;

                row2.Append(
                        new Cell
                        {
                            DataType = CellValues.String,
                            CellValue = new CellValue("")
                        },
                         new Cell
                         {
                             DataType = CellValues.String,
                             CellValue = new CellValue("Số lượng")
                         },
                         new Cell
                         {
                             DataType = CellValues.String,
                             CellValue = new CellValue("Tỉ lệ đạt")
                         }
                );


                var row3 = new Row();
                row3.RowIndex = 4;



                row3.Append(
                      new Cell
                      {
                          DataType = CellValues.String,
                          CellValue = new CellValue("Tổng số lượng CV")
                      },
                       new Cell
                       {
                           DataType = CellValues.Number,
                           CellValue = new CellValue(sumTotalCV)
                       },
                       new Cell
                       {
                           DataType = CellValues.String,
                           CellValue = new CellValue("10%")
                       }

              );

                var row4 = new Row();
                row4.RowIndex = 5;

                var rateOnboard = "0";
                if (sumTotalCV > 0 && totalOnboard > 0)
                {
                    rateOnboard = (totalOnboard / sumTotalCV).ToString("0.00%");
                }
                row4.Append(
                   new Cell
                   {
                       DataType = CellValues.String,
                       CellValue = new CellValue("Ứng viên Onboard")
                   },
                    new Cell
                    {
                        DataType = CellValues.Number,
                        CellValue = new CellValue(totalOnboard)
                    },
                    new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(rateOnboard)
                    });


                var rateCvNew = "0";
                if (sumTotalCV > 0 && totalCVNew > 0)
                {
                    rateCvNew = (totalCVNew / sumTotalCV).ToString("0.00%");
                }

                var row7 = new Row();
                row7.RowIndex = 7;

                row7.Append(
                   new Cell
                   {
                       DataType = CellValues.String,
                       CellValue = new CellValue("CV Nguồn")
                   },
                    new Cell
                    {
                        DataType = CellValues.Number,
                        CellValue = new CellValue(totalCVNew)
                    },
                    new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(rateCvNew)
                    });


                var rateCVProcess = "0";
                if (sumTotalCV > 0 && totalCVProcess > 0)
                {
                    rateCVProcess = (totalCVProcess / sumTotalCV).ToString("0.00%");
                }
                var row8 = new Row();
                row8.RowIndex = 8;

                row8.Append(
                   new Cell
                   {
                       DataType = CellValues.String,
                       CellValue = new CellValue("Ứng viên đang process")
                   },
                    new Cell
                    {
                        DataType = CellValues.Number,
                        CellValue = new CellValue(totalCVProcess)
                    },
                    new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(rateCVProcess)
                    });

                var row9 = new Row();

                row9.RowIndex = 9;

                row9.Append(
                   new Cell
                   {
                       DataType = CellValues.String,
                       CellValue = new CellValue("Ứng viên Onboard")
                   },
                    new Cell
                    {
                        DataType = CellValues.Number,
                        CellValue = new CellValue(totalOnboard)
                    },
                    new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(rateOnboard)
                    });


                var rateCVDone = "0";
                if (sumTotalCV > 0 && cvDone > 0)
                {
                    rateCVDone = (cvDone / sumTotalCV).ToString("0.00%");
                }
                var row10 = new Row();
                row10.RowIndex = 10;

                row10.Append(
                   new Cell
                   {
                       DataType = CellValues.String,
                       CellValue = new CellValue("Ứng viên Done")
                   },
                    new Cell
                    {
                        DataType = CellValues.Number,
                        CellValue = new CellValue(cvDone)
                    },
                    new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(rateCVDone)
                    });
                sheetData.Append(row1);
                sheetData.Append(rowform2);

                sheetData.Append(row2);
                sheetData.Append(row3);
                sheetData.Append(row4);

                sheetData.Append(row7);
                sheetData.Append(row8);
                sheetData.Append(row9);
                sheetData.Append(row10);

                //status Detail

                var row12 = new Row();
                row12.RowIndex = 12;

                row12.Append(
                   new Cell
                   {
                       DataType = CellValues.String,
                       CellValue = new CellValue("Thông số theo trạng thái")
                   });

                var row13 = new Row();
                row13.RowIndex = 13;

                row13.Append(
                        new Cell
                        {
                            DataType = CellValues.String,
                            CellValue = new CellValue("Trạng thái")
                        },
                         new Cell
                         {
                             DataType = CellValues.String,
                             CellValue = new CellValue("Số lượng")
                         },
                         new Cell
                         {
                             DataType = CellValues.String,
                             CellValue = new CellValue("Tỉ lệ đạt")
                         }

                );
                sheetData.Append(row12);
                sheetData.Append(row13);
                var rowindex = 14;
                foreach (var itemOrder in allRowStatus.Data)
                {
                    var rowinfo = new Row();
                    rowinfo.RowIndex = (uint)rowindex;

                    var itemRow = itemOrder as dynamic;
                    var rateItem = "0";
                    var nameStatus = itemRow.Name;
                    var sumTotal = itemRow.Total;
                    if (itemRow.Total > 0 && sumTotalCV > 0)
                    {
                        rateItem = ((sumTotal / sumTotalCV)).ToString("0.00%");
                    }
                    rowinfo.Append(
                        new Cell
                        {
                            DataType = CellValues.String,
                            CellValue = new CellValue(nameStatus)
                        },
                         new Cell
                         {
                             DataType = CellValues.String,
                             CellValue = new CellValue(sumTotal)
                         },
                         new Cell
                         {
                             DataType = CellValues.String,
                             CellValue = new CellValue(rateItem)
                         }

                    );
                    sheetData.Append(rowinfo);
                    rowindex++;

                }


                var rowDetail = new Row();
                rowDetail.RowIndex = 1;

                rowDetail.Append(
                    new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue("Ngày thống kê")
                    },
                     new Cell
                     {
                         DataType = CellValues.String,
                         CellValue = new CellValue(DateTime.Now.ToString("dd.MM.yyyy"))
                     }

                );
                sheetDataOrder.Append(rowDetail);

                var rowform = new Row();
                rowform.RowIndex = 2;

                rowform.Append(
                    new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue("Từ ngày")
                    },
                     new Cell
                     {
                         DataType = CellValues.String,
                         CellValue = new CellValue(fromText)
                     },
                     new Cell
                     {
                         DataType = CellValues.String,
                         CellValue = new CellValue("Đến ngày")
                     },
                     new Cell
                     {
                         DataType = CellValues.String,
                         CellValue = new CellValue(toText)
                     }

                );

                sheetDataOrder.Append(rowform);
                var listString = new string[]{"STT","Ngày tạo", "TC nhận ngày", "Mã đơn hàng", "ứng cử viên",
                   "Vị trí", "Đối tác", "Dự án", "Link tài liệu","Link cV", "ghi chú đầu vào",
                   "Trạng thái", "Người tạo", "người đang xư lý", "Ghi chú TC", "Kết quả tuyển dụng"
                };
                var roworderHeadedr = new Row();
                roworderHeadedr.RowIndex = 4;

                foreach (var itemStr in listString)
                {
                    var columnText = new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(itemStr)
                    };
                    roworderHeadedr.Append(columnText);
                }
                sheetDataOrder.Append(roworderHeadedr);
                int indexRow = 5;

                foreach (var itemStr in allOrder.Data)
                {
                    var rowInfo = new Row();
                    var itemDraw = itemStr as dynamic;

                    var dateGetText = itemDraw.DateGet;
                    rowInfo.RowIndex = (uint)indexRow;
                    rowInfo.Append(new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue((indexRow - 4).ToString())
                    });

                    rowInfo.Append(new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(itemDraw.CreateAt.ToString("dd:MM:yyyy HH:MM"))
                    });

                    rowInfo.Append(new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(itemDraw.DateGet)
                    });
                    rowInfo.Append(new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(itemDraw.Code)
                    });



                    rowInfo.Append(new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(itemDraw.CandidateFullName)
                    });

                    rowInfo.Append(new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(itemDraw.PositionText)
                    });

                    rowInfo.Append(new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(itemDraw.PartnerName)
                    });

                    rowInfo.Append(new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(itemDraw.ProjectName)
                    });
                    rowInfo.Append(new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(itemDraw.ShortDes)
                    });


                    rowInfo.Append(new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(itemDraw.CVLink)
                    });


                    rowInfo.Append(new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(itemDraw.Noted)
                    });

                    rowInfo.Append(new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(itemDraw.StatusText)
                    });

                    rowInfo.Append(new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(itemDraw.AuthorName)
                    });

                    rowInfo.Append(new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(itemDraw.AssigeeName)
                    });

                    rowInfo.Append(new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue("")
                    });
                    var resultTD = "Chưa khai thác";
                    if (itemDraw.Assignee > 0)
                    {
                        resultTD = "Đang Process";
                    }

                    if (itemDraw.Result < 1)
                    {


                    }
                    else if (itemDraw.Result == 1)
                    {
                        resultTD = "Onboard";
                    }
                    else if (itemDraw.Result == 2)
                    {
                        resultTD = "Done";
                    }

                    rowInfo.Append(new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(resultTD)
                    });
                    indexRow++;
                    sheetDataOrder.Append(rowInfo);
                }

                document.Save();

            }

            return "report" + "/" + userName + "/" + fileName;

        }
    }
}
