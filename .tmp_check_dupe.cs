using System;
using System.Data.SqlClient;
class P {
  static void Main() {
    var cs = "Server=115.79.5.244,1433;Database=humandev;User ID=sa;Password=bigstep123!@#;TrustServerCertificate=True;";
    using var con = new SqlConnection(cs);
    con.Open();
    var cmd = new SqlCommand(@"
SELECT c.Id, c.UserName, c.Name, c.Phone, c.Email, c.CreateAt,
       (SELECT COUNT(1) FROM ScheduleInterview s WHERE s.RelId = c.Id AND ISNULL(s.Deleted,0)=0) AS ScheduleCount,
       (SELECT COUNT(1) FROM CandidateActivity a WHERE a.RelId = c.Id AND ISNULL(a.Deleted,0)=0) AS ActivityCount,
       (SELECT COUNT(1) FROM Notification n WHERE n.RelId = c.Id AND ISNULL(n.Deleted,0)=0) AS NotificationCount
FROM Candidate c
WHERE c.Id IN (5762,5763);", con);
    using var r = cmd.ExecuteReader();
    while (r.Read()) {
      Console.WriteLine($"Id={r[0]}, UserName={r[1]}, Name={r[2]}, Phone={r[3]}, Email={r[4]}, CreateAt={r[5]}, ScheduleCount={r[6]}, ActivityCount={r[7]}, NotificationCount={r[8]}");
    }
  }
}
