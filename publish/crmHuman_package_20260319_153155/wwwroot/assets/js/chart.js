

const ctx = document.getElementById('myChart');
const ctx2 = document.getElementById('myChart2');



function drawChartLine ( dataDraw) 
{

  let lableTc2 = [];
  let data2 =[];

  for (let index = 0; index < dataDraw.length; index++) {

        let itemDraw = dataDraw[index];
        var tempDate = new Date(itemDraw.dateCreate);

        var text = tempDate.getDate()+ "/" + tempDate.getMonth()
        lableTc2.push(text);
        data2.push(itemDraw.total);

   }
  
   const setup = {
    labels: lableTc2,
    datasets: [
      {
        label: 'Tổng số CV',
        data: data2,
        fill: false,
        borderColor: 'rgb(75, 192, 192)',
        tension: 0.1
    },
    {
      label: 'SL ứng tuyển',
      data: data2,
      fill: false,
      borderColor: 'rgb(100, 20, 192)',
      tension: 0.1
  },

  {
    label: 'SL onboard',
    data: data2,
    fill: false,
    borderColor: 'rgb(200, 20, 192)',
    tension: 0.1
}
  ]
  };
  new Chart(ctx3, {
    type: 'line',
    data: setup,
  
  });
 

  
}



function drawChartLineAllCV ( dataDraw, typeDraw =1) 
{
  debugger;
  var idChart ="myChart3";
  var titleChart = "SL CV";
  var borderColor= "rgb(75, 192, 192)";

  if( typeDraw ==2)
  {
    idChart ="myChart4";
    titleChart = "CV ứng tuyển";
    borderColor ="rgb(0,0,255)";
  }

  
  if( typeDraw ==3)
    {
      idChart ="myChart5";
      titleChart = "SL CV onboard";
      borderColor ="rgb(178,34,34)";
    }
  


   DrawLineChart(idChart,dataDraw,titleChart,borderColor);
   
 
}

function DrawLineChart(idChart, dataDraw = [], titleChart ="Số lượng CV", colorBorder ="rgb(75, 192, 192)")
{

  
  var listDataAll =[];
  for (let index = 0; index < dataDraw.length; index++) {
   const itemDraw = dataDraw[index];

   var sumK =0 ;
   var tempIte = itemDraw[0];
   for (let k = 0; k < itemDraw.length; k++) {
       let itemK = itemDraw[k];
       sumK += itemK.total;
     }
   tempIte.total =sumK;
   listDataAll.push(tempIte);

  }


  let lableTc2 = [];
  let data2 =[];

  for (let index = 0; index < listDataAll.length; index++) {

        let itemDraw = listDataAll[index];
        var tempDate = new Date(itemDraw.dateCreate);

        var text = tempDate.getDate()+ "/" +  (tempDate.getMonth() +1);
        lableTc2.push(text);
        data2.push(itemDraw.total);

   }

   const setupAllCV = {
    labels: lableTc2,
    datasets: [
      {
        label: titleChart,
        data: data2,
        fill: false,
        borderColor: colorBorder,
        tension: 0.1
      
    }
  ]



  };

  
  var chartEle = document.getElementById(idChart);
  if(chartEle)
  {
      new Chart(chartEle, {
        type: 'line',
        data: setupAllCV,
        options: {
          onClick: (event, elements, chart) => {
            if (elements[0]) {            
               const i = elements[0].index;

               console.log(dataDraw[[i]]);
               alert(i);
               console.log(chart.data.datasets[0]);
               console.log(chart);
               alert(chart.data.labels[i] + ': ' + chart.data.datasets[0].data[i]);
            }
          }
        }
      
      });
  }
  return;
}


function drawChartAll ( dataDraw) 
{
  let lableTc2 = [];
  let data2 =[];

  for (let index = 0; index < dataDraw.length; index++) {

        let itemDraw = dataDraw[index];
        var tempDate = new Date(itemDraw.dateCreate);

        var text = tempDate.getDate()+ "/" + tempDate.getMonth()
        lableTc2.push(text);
        data2.push(itemDraw.total);

   }
  
   const setup = {
    labels: lableTc2,
    datasets: [
      {
        label: 'Tổng số CV',
        data: data2,
        fill: false,
        borderColor: 'rgb(75, 192, 192)',
        tension: 0.1
    },
    {
      label: 'SL ứng tuyển',
      data: data2,
      fill: false,
      borderColor: 'rgb(100, 20, 192)',
      tension: 0.1
  },

  {
    label: 'SL onboard',
    data: data2,
    fill: false,
    borderColor: 'rgb(200, 20, 192)',
    tension: 0.1
}
  ]
  };
  new Chart(ctx3, {
    type: 'line',
    data: setup,
  
  });
 

  
}




function r() {
  return Math.floor(Math.random() * 256);
}

function drawChartBasic(dataDraw)
{
      var laybles =[];
      var dataChart =[];
      var backgroudColorChart =[];
      for (let index = 0; index < dataDraw.length; index++) {
        const colorText = "rgb(" + r() + "," + r() + "," + r() + ")";
          const itemPostsion = dataDraw[index];
          dataChart.push(itemPostsion.length);
        
          if( itemPostsion.length >0)
          {
        
            var itemchild = itemPostsion[0];
            laybles.push(itemchild.assigeeName);
          }
      
          backgroudColorChart.push(colorText);
      }
      
        const data = {
        labels: laybles,
        datasets: [{
          label: 'Số lượng:',
          data: dataChart,
          backgroundColor:backgroudColorChart,
          hoverOffset: 4
        }]
      };
      new Chart(ctx, {
        type: 'pie',
        data: data,
        options: {
          plugins: {
              legend: {
                  display: false
                
              }
              
          },
          onClick: (event, elements, chart) => {
            const i = elements[0].index;
            openFormModalChart(dataDraw[i]);
          }
      }
      
      });
    
}

function renderChartCV(dataSource)
{   
  // console.log(dataSource)
  var htmlText = '';
  document.getElementById("tableDataChart").innerHTML ="";
 


  for (let index = 0; index < dataSource.length; index++) {

    

      htmlText +=  '<tr>';
      let dataItem = dataSource[index];
      var targetLink = "/OrderDetailNew?OrderId=" + dataItem.id;
      htmlText+= "<td>"+ (index+ 1) +"</td>";
      htmlText+= "<td>"+   "<a href= '"+ targetLink +"' target = '_blank'>" + dataItem.code + "</a>" +"</td>";
      htmlText+= "<td>"+ dataItem.candidateFullName+"</td>";
      htmlText+= "<td>"+ dataItem.positionText+"</td>";

      var textProject = "";
      if( dataItem.projectName !=  null)
      {
        textProject += dataItem.projectName;
      }
      
      if( dataItem.projectName != null && dataItem.projectName && dataItem.partnerName != null &&  dataItem.partnerName != "")
      {
        textProject += "/";
      }

      if( dataItem.partnerName !=  null)
        {
          textProject += dataItem.partnerName;
        }
    if( textProject == null)
    {
      textProject ="";
    }
      

      htmlText+= "<td>"+ textProject +"</td>";
      htmlText+= "<td>"+ dataItem.statusText+"</td>";
      htmlText+= "<td>"+ dataItem.authorName+"</td>";
      htmlText+= "<td>"+ dataItem.assigeeName+"</td>";
      htmlText+= "<td>"+ dataItem.dateGet+"</td>";
      htmlText+= "<td>"+ dataItem.updateAt+"</td>";
     
      htmlText +='</tr>';
  }
  document.getElementById("tableDataChart").innerHTML  += htmlText;


}

 function openFormModalChart(itemDAta)
{
   renderChartCV(itemDAta);
    $('#exampleModalCenter').modal('show'); 
}

function drawChartApply(dataDraw)
{
   var laybles =[];
   var dataChart =[];
   var backgroudColorChart =[];
   for (let index = 0; index < dataDraw.length; index++) {
    const colorText = "rgb(" + r() + "," + r() + "," + r() + ")";
      const itemPostsion = dataDraw[index];
      dataChart.push(itemPostsion.length);

      if( itemPostsion.length >0)
      {
        
        var itemchild = itemPostsion[0];
        laybles.push(itemchild.assigeeName);
      }
  
      backgroudColorChart.push(colorText);
   }
   
    const data = {
    labels: laybles,
    datasets: [{
      label: 'Số lượng:',
      data: dataChart,
      backgroundColor:backgroudColorChart,
      hoverOffset: 4
    }]
  };
  new Chart(ctx2, {
    type: 'pie',
    data: data,
    options: {
      plugins: {
          legend: {
              display: false
            
          }
          
      },
      onClick: (event, elements, chart) => {
        // const i = elements[0].index;
        // // alert(chart.data.labels[i] + ': ' + chart.data.datasets[0].data[i]);
        // openFormModal();

        const i = elements[0].index;
        openFormModalChart(dataDraw[i]);
      }
  }
  
  });
    
}


function DrawChart()
{
  const data = {
    labels: [
      'Red',
      'Blue',
      'Yellow'
    ],
    datasets: [{
      label: 'My First Dataset',
      data: [300, 50, 100],
      backgroundColor: [
        'rgb(255, 99, 132)',
        'rgb(54, 162, 235)',
        'rgb(255, 205, 86)'
      ],
      hoverOffset: 4
    }]
  };
  new Chart(ctx, {
    type: 'pie',
    data: data,
    options: {
      plugins: {
          legend: {
              display: false
            
          }
          
      },
      onClick: (event, elements, chart) => {
        const i = elements[0].index;
        alert(chart.data.labels[i] + ': ' + chart.data.datasets[0].data[i]);
      }
  }
  
  });
  
}



function groupBy(collection, property) {
  var i = 0, val, index,
      values = [], result = [];
  for (; i < collection.length; i++) {
      val = collection[i][property];
      index = values.indexOf(val);
      if (index > -1)
          result[index].push(collection[i]);
      else {
          values.push(val);
          result.push([collection[i]]);
      }
  }
  return result;
}


document.addEventListener('DOMContentLoaded', function() {

  var dataOrderDraft =   groupBy(dataDraw.listOrderDraft, "assigeeName");
     
   var dataOrderApply =  groupBy(dataDraw.listOrderApply, "assigeeName");

   var dataCountGroupByDate = groupBy(dataDraw.allCountCVChartAllCV.data,"userName");
    var dataCountGroupByDateALl = groupBy(dataDraw.allCountCVChartAllCV.data,"dateCreate");

    var dataCountGroupByDateApply = groupBy(dataDraw.allCountCVApply.data,"dateCreate");
    var dataCountallCountCVOnboard = groupBy(dataDraw.allCountCVOnboard.data,"dateCreate");
    drawChartBasic(dataOrderDraft);
    drawChartApply(dataOrderApply);
    drawChartLineAllCV(dataCountGroupByDateALl);

    drawChartLineAllCV(dataCountGroupByDateApply,2);
    drawChartLineAllCV(dataCountallCountCVOnboard,3);

    
}, false);


function getMonday(d) {
  // var day = d.getDay(),
  //     diff = d.getDate() - day + (day == 0 ? -6:1); // adjust when day is sunday
  // d.setDate(diff);
  // d.setHours(0,0,0,0); // set hours to 00:00:00

  // return d; // object is mutable no need to recreate object
   

  
}


function changeTypeTime(itemSelect)
{
    
    var valueSlect = itemSelect.value;

    var timeBegin = new Date();
    var  endBegin =  new Date();
 

    var textlable = " THÁNG NÀY";
    if( valueSlect == 1 )
    {
       timeBegin =  new Date(timeBegin.setDate(timeBegin.getDate() - timeBegin.getDay() +1 ));
       endBegin  = new Date(endBegin.setDate(endBegin.getDate() - endBegin.getDay()+7));
       textlable=  "TUẦN NÀY";

    }

    
    if( valueSlect == 3 )
      {
        var date = new Date();
        
        y = date.getFullYear(), m = date.getMonth();
        timeBegin = new Date(y, m, 2);
        endBegin = new Date(y, m + 1, 1);

      
      }

      document.getElementById("lableText").textContent   = textlable;

    if( valueSlect == 2)
    {
      document.getElementById('fromDate').value = null;
      document.getElementById('toDate').value = null;
      textlable = "TỪ NGÀY ĐẾN NGÀY"
      document.getElementById("lableText").textContent   = textlable;
      return;
    }
    

    var tiembeginSet = timeBegin.toISOString().substring(0,10);
    document.getElementById('fromDate').value = tiembeginSet;

    var timeEndSet = endBegin.toISOString().substring(0,10);
    document.getElementById('toDate').value = timeEndSet;

  
}

function openCVOnboard(modeLoad) 
{

  $('#dataOrderMember').modal('show'); 

  var htmlText = '';
  document.getElementById("tableDataChart").innerHTML ="";

  var dataSources = dataDraw.allOnboardCV.data;
  let rowIndexSTT =0;
 
  for (let index = 0; index < dataSources.length; index++) {


      htmlText +=  '<tr>';
      let dataItem = dataSources[index];

      if(modeLoad ==1)
      {
        if( dataItem.result !=1)
        {
          continue;
        }
      }
      rowIndexSTT ++;
      var targetLink = "/OrderDetailNew?OrderId=" + dataItem.orderId;
      htmlText+= "<td>"+ (rowIndexSTT) +"</td>";
      htmlText+= "<td>"+   "<a href= '"+ targetLink +"' target = '_blank'>" + dataItem.orderCode + "</a>" +"</td>";

      htmlText+= "<td>"+ dataItem.candidateFullName+"</td>";
      htmlText+= "<td>"+ dataItem.onboardDate+"</td>";
      var textjob = "";
      if( dataItem.positionText !=  null)
        {
          textjob += dataItem.positionText;
        }

      htmlText+= "<td>"+ textjob+"</td>";

      var textProject = "";
      if( dataItem.projectName !=  null)
      {
        textProject += dataItem.projectName;
      }
      
      if( dataItem.projectName != null && dataItem.projectName && dataItem.partnerName != null &&  dataItem.partnerName != "")
      {
        textProject += "/";
      }

      if( dataItem.partnerName !=  null)
        {
          textProject += dataItem.partnerName;
        }

      htmlText+= "<td>"+ textProject +"</td>";
      htmlText+= "<td>"+ dataItem.statusText+"</td>";
      htmlText+= "<td>"+ dataItem.systemStatusText+"</td>";

      htmlText+= "<td>"+ dataItem.authorName+"</td>";
      htmlText+= "<td>"+ formatDateTime(dataItem.createAt)+"</td>";
      htmlText+= "<td>"+ dataItem.dpd+"</td>";
      htmlText+= "<td>"+ dataItem.warrantydate+"</td>";
      htmlText+= "<td>"+ dataItem.assigeeName+"</td>";

      htmlText+= "<td>"+ getRessult(dataItem.result)+"</td>";
     
      htmlText +='</tr>';
  }
  document.getElementById("dataOnboardMemberTable").innerHTML  += htmlText;

}

function closeFormUserCharOnboard()
{
  $('#dataOrderMember').modal('hide'); 
}

function getRessult ( resultStatus)
{
  if(resultStatus == "0")
  {
    return 'Trong bảo hành';
  }
  if(resultStatus == "1")
    {
      return 'Pass';
    }

    if(resultStatus == "2")
      {
        return 'Failed';
      }
}