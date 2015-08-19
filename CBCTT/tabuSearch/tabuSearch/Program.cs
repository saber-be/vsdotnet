using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace tabuSearch
{

    class Program
    {

        static void Main(string[] args)
        {
            //در صورتی که آدرس فایل ورودی و خروجی به عنوان پارامتر ورودی 
            //در زمان اجرای برنامه داده شده باشند این شرط برقرار خواهد بود
            //و دیگر نیازی به پرسیدن آدرس آنها از کاربر نیست
            if (args.Length > 1)
            {
                tabuSolver solver = new tabuSolver(args[0], args[1]);
            }
            else
            {
                Console.WriteLine("input name:");
                string input = Console.ReadLine();
                Console.WriteLine("output name:");
                string output = Console.ReadLine();
                tabuSolver solver = new tabuSolver(input, output);
            }
        }
    }
    class tabuSolver
    {
        /// <summary>
        /// جریانی که فایل ورودی مساله را می خواند
        /// </summary>
        StreamReader IReader;
        /// <summary>
        /// جریانی که برای نوشتن خروجی در فایل بکار میرود
        /// </summary>
        StreamWriter OWriter;
        /// <summary>
        /// An instance of CB-CTT
        /// </summary>
        problemInstance prblm = new problemInstance();
        solution X0 = new solution();
        /// <summary>
        /// بهترین جواب
        /// </summary>
        solution Xstar;
        solution Xprime;
        solution XstarPrime;
        /// <summary>
        /// θ in article
        /// </summary>
        public int theta;
        public int theta0;
        /// <summary>
        /// ξ in article -- is pronounced [ksi] :D
        /// </summary>
        public int Xi;
        /// <summary>
        /// η in article
        /// </summary>
        public int Eta;
        public int landa;
        public int EtaMin;
        public int EtaMax;


        int pomov;
        int impomov;
        List<simpleMove> simpleSwapTabuList;
        List<int[]> simpleSwapList;
        List<kempeSwap> kempeSwapTabuList;

        /// <summary>
        /// تابع سازنده کلاس که شروع به حل مساله میکند
        /// </summary>
        /// <param name="input">نام فایل ورودی</param>
        /// <param name="output">نام فایل خروجی</param>
        public tabuSolver(string input, string output)
        {
            pomov = 0;
            impomov = 0;
            try
            {
                IReader = new StreamReader(input);
                OWriter = new StreamWriter(output);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("press any to exit...");
                Console.Read();
                return;
            }
            DateTime startRuntime = new DateTime();
            startRuntime = DateTime.Now;
            readInput(out prblm);//Algorithm 3 line 1
            Xi = 0;
            theta = theta0 = 10;
            Eta = EtaMin = 4;
            EtaMax = 15;
            simpleSwapTabuList = new List<simpleMove>();
            kempeSwapTabuList = new List<kempeSwap>();
            X0.initSol(prblm);
            int[] pos = new int[2];
            do
            {
                int ci=X0.HR1();
                pos = X0.HR2(ci);
                X0.insertLecture(ci, pos[0], pos[1]);
                OWriter.WriteLine(prblm.courses[ci].CourseID + "    " + prblm.Rooms[pos[1]].ID + " " + pos[0]/prblm.Periods_per_day+" " + pos[0]%prblm.Periods_per_day);
                Console.WriteLine("ci:"+ci+"    "+pos[0] + "," + pos[1]);
            } while (X0.LC.Count > 0 && pos[0] != -1);//end of part1: initialization

            Xstar = new solution();
            solutionCopier.copy(X0, out Xstar);
            solutionCopier.copy(TS(X0, theta), out Xstar);
            
            int loopCount1 = 100;
            int loopCount2 = 100;
            do
            {
                loopCount1 = 100;
                solutionCopier.copy(perturb(Xstar,Eta),out Xprime);
                solutionCopier.copy( TS(Xprime, theta),out  XstarPrime);
                if(validator.getCost(prblm,XstarPrime)<validator.getCost(prblm,Xstar)+2){
                    do
                    {
                        theta = (int )(1.6 * theta0);
                        solutionCopier.copy(TS(Xprime, theta), out  XstarPrime);
                        
                    } while (loopCount1-->0);
                }
                if (validator.getCost(prblm, XstarPrime) < validator.getCost(prblm, Xstar))
                {
                    solutionCopier.copy(XstarPrime, out Xstar);
                    theta = theta0;
                    Eta = EtaMin;

                }
                else
                {
                    theta = theta0;
                    Xi++;
                    Eta = Math.Max(EtaMin + landa * Xi, EtaMax);
                }
                Console.WriteLine(validator.getCost(prblm, Xstar));
            } while (loopCount2-->0);
            
            OWriter.Close();
            DateTime endRuntime = new DateTime();
            endRuntime = DateTime.Now;
            Console.WriteLine("{0} possible moves \n{1} impossible move \n",pomov,impomov);
            Console.WriteLine("run time = {0}",endRuntime-startRuntime);
            Console.WriteLine("press any to exit...");
            Console.Read();

        }
        /// <summary>
        /// read input file and init problemInstance p
        /// </summary>
        /// <param name="p">مساله ای که در حال پیدا کردن جواب آن هستیم</param>
        public void readInput(out problemInstance p)
        {
            problemInstance tempP = new problemInstance();
            string line;
            string[] parts;
            //read name
            line = IReader.ReadLine();
            parts = line.Split(' ');
            tempP.Name = parts[1];

            //read Courses num
            line = IReader.ReadLine();
            parts = line.Split(' ');
            tempP.CoursesNum = int.Parse(parts[1]);
            tempP.courses = new course[int.Parse(parts[1])];

            //read Rooms
            line = IReader.ReadLine();
            parts = line.Split(' ');
            tempP.RoomsNum = int.Parse(parts[1]);
            tempP.Rooms = new room[int.Parse(parts[1])];

            //read Days num 
            line = IReader.ReadLine();
            parts = line.Split(' ');
            tempP.Days = int.Parse(parts[1]);

            //read Periods_per_day num
            line = IReader.ReadLine();
            parts = line.Split(' ');
            tempP.Periods_per_day = int.Parse(parts[1]);

            //read Curricula num
            line = IReader.ReadLine();
            parts = line.Split(' ');
            tempP.CurriculaNum = int.Parse(parts[1]);
            tempP.Curricula = new Curriculum[int.Parse(parts[1])];

            //read Constraints num
            line = IReader.ReadLine();
            parts = line.Split(' ');
            tempP.ConstraintsNum = int.Parse(parts[1]);
            tempP.Constraints = new UNAVAILABILITY_CONSTRAINT[int.Parse(parts[1])];

            //read courses list
            while (line != "COURSES:") line = IReader.ReadLine();
            for (int i = 0; i < tempP.courses.Length; i++)
            {
                line = IReader.ReadLine();
                parts = line.Split(' ');
                tempP.courses[i].CourseID = parts[0];
                tempP.courses[i].Teacher = parts[1];
                tempP.courses[i].Lectures = int.Parse(parts[2]);
                tempP.courses[i].MinWorkingDays = int.Parse(parts[3]);
                tempP.courses[i].Students = int.Parse(parts[4]);
            }
            //read rooms list
            while (line != "ROOMS:")
                line = IReader.ReadLine();
            for (int i = 0; i < tempP.Rooms.Length; i++)
            {
                line = IReader.ReadLine();
                parts = line.Split(' ');
                tempP.Rooms[i].ID = parts[0];
                tempP.Rooms[i].Capacity = int.Parse(parts[1]);
            }
            //read  Curriculums list
            while (line != "CURRICULA:")
                line = IReader.ReadLine();
            for (int i = 0; i < tempP.Curricula.Length; i++)
            {
                line = IReader.ReadLine();
                parts = line.Split(' ');
                tempP.Curricula[i].ID = parts[0];
                tempP.Curricula[i].Courses = parts.Skip(2).Take(parts.Length - 2).ToArray();
            }

            //read Constraints list
            while (line != "UNAVAILABILITY_CONSTRAINTS:")
                line = IReader.ReadLine();
            for (int i = 0; i < tempP.Constraints.Length; i++)
            {
                line = IReader.ReadLine();
                parts = line.Split(' ');
                tempP.Constraints[i].CourseID = parts[0];
                tempP.Constraints[i].Day = int.Parse(parts[1]);
                tempP.Constraints[i].Day_Period = int.Parse(parts[2]);
            }
            IReader.Close();//end of file.
            p = tempP;
        }

        public solution TS(solution X,int theta)
        {
            int loops = theta;
            solution Xbest = new solution();
            solutionCopier.copy(X, out Xbest);//make a deep copy of X to Xbest
            simpleSwapList=new List<int[]>();
            int r1, r2, t1, t2;
            int len = X.timeTable.Length * X.timeTable[0].Length;
            for (int i = 0; i < len; i++)
            {
                for (int j = i + 1; j < len; j++)
                {
                    /// <summary>
                    /// [0]:t1
                    /// [1]:r1
                    /// [2]:t2
                    /// [3]:r2
                    /// </summary>
                     int[] simswap = new int[4];
                        t1 = simswap[0] = i / X.timeTable[0].Length;
                        r1 = simswap[1] =  i % X.timeTable[0].Length;
                        t2 = simswap[2] = j / X.timeTable[0].Length;
                        r2 = simswap[3] =  + j % X.timeTable[0].Length;
                        string course1 = X.timeTable[t1][r1].CourseID;
                        string course2 = X.timeTable[t2][r2].CourseID;
                        simpleMove m1 = new simpleMove();
                        simpleMove m2 = new simpleMove();
                    
                        m1.courseId = course1;
                        m1.r = r2;
                        m1.t = t2;
                        
                        m2.courseId = course2;
                        m2.r = r1;
                        m2.t = t1;
                    
                    if( isAvl(X,m1) && isAvl(X,m2) )
                    {
                        if (m1.courseId != null || m2.courseId != null)
                        simpleSwapList.Add(simswap);
                       // if (m1.courseId != null && m2.courseId != null)
                            //Console.WriteLine("{0} , {1}",m1.courseId, m2.courseId);
                    }
                    
                   
                }
            }
            do
            {
                
                solutionCopier.copy(TSN1(X, theta),out Xstar);
                solutionCopier.copy(TSN2(Xstar, (int)(theta / 3)),out XstarPrime);


                double costXstarPrime, costXbest;
                costXbest = validator.getCost(prblm, Xbest);
                costXstarPrime = validator.getCost(prblm, XstarPrime);
                if (costXstarPrime < costXbest)
                {
                   // Console.Write("{0} < {1} < {2}\n",validator.getCost( prblm,Xstar), costXstarPrime,costXbest);
                    solutionCopier.copy( XstarPrime,out Xbest );
                    solutionCopier.copy( XstarPrime,out X);
                }
            } while (loops-->0);

            return Xbest;
        }//end of TS

        public solution TSN1(solution X, int theta)
        {

            solution tempX = new solution();
            solutionCopier.copy(X, out tempX);

            Random r = new Random();
            //int index = r.Next(simpleSwapList.Count);
            do
            {
                if (simpleSwapList.Count > 0)
                {
                    int index = r.Next(simpleSwapList.Count);
                    swap(tempX, simpleSwapList[index]);
                }

            } while (theta-->0);
            double cost1=validator.getCost(prblm,tempX);
            double cost2=validator.getCost(prblm,Xstar);
            if(cost1<cost2)
                return tempX;
            return Xstar;

        }

        public solution TSN2(solution X, int theta)
        {

            solution tempX = new solution();
            solutionCopier.copy(X, out tempX);

            do
            {

            } while (theta-->0);

            return tempX;

        }

        public solution perturb(solution Xstar, int Eta) 
        {
            return Xstar;
        }


        public bool isAvl(solution X, simpleMove move){
            if (move.courseId == null)  return true;
            foreach (var item in X.Constraints)
            {
                int t=item.Day*prblm.Periods_per_day +item.Day_Period;
                if(item.CourseID==move.courseId &&  t==move.t) return false;
                
            }
            

            return true;
        }

        public void swap(solution X, simpleMove m1,simpleMove m2)
        {
            course c1 = X.timeTable[m1.t][m1.r];
            course c2 = X.timeTable[m2.t][m2.r];
            X.timeTable[m1.t][m1.r]=c2;
            X.timeTable[m2.t][m2.r]=c1;

            X.setNr(m1.courseId);
            X.setNr(m2.courseId);

            
            
        }


        public bool isMoveAv(solution X,string cId,int period) {

            List<string> relatedCourse = new List<string>();
            foreach (var item in prblm.Curricula)
            {
                if (item.Courses.Contains(cId))
                {
                    foreach (var course in item.Courses)
                    {
                     if(course!="" && !relatedCourse.Contains(course) )   relatedCourse.Add(course);
                    }
                }
            }


            for (int i = 0; i < X.timeTable[period].Length; i++)
            {
                foreach (var item in X.timeTable[period])
                {
                    if (relatedCourse.Contains(item.CourseID))//this move is not possible!!!
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        public void swap(solution X,int[] swap)
        {



            course c1 = X.timeTable[swap[0]][swap[1]];
            course c2 = X.timeTable[swap[2]][swap[3]];

            if (c1.CourseID != null)//mojaz bodan enteqal ra barasi kon
            {
                if (!isMoveAv(X, c1.CourseID, swap[2]))
                {
                   // Console.WriteLine("{0} can't move to {1}",c1.CourseID,swap[2]);
                    impomov++;
                    return;
                }
            }
            if (c2.CourseID != null)//mojaz bodan enteqal ra barasi kon
            {
                if (!isMoveAv(X, c2.CourseID, swap[0]))
                {
                   // Console.WriteLine("{0} can't move to {1}", c2.CourseID, swap[0]);
                    impomov++;
                    return;
                }
            }
            pomov++;

            X.timeTable[swap[0]][swap[1]]=c2;
            X.timeTable[swap[2]][swap[3]]=c1;

            int c1Index,c2Index;
            c1Index = c2Index = 0;
            for (int i = 0; i < prblm.courses.Length; i++)
                {
                    if (prblm.courses[i].CourseID == c1.CourseID) c1Index = i;
                    if (prblm.courses[i].CourseID == c2.CourseID) c2Index = i;
                }

            if (c1.CourseID != null)
            {
                simpleMove m1 = new simpleMove();
                m1.courseId = c1.CourseID;
                m1.t = swap[2];
                m1.r = swap[3];


                X.setNr(c1.CourseID);
    
                X.setNd(c1Index);
                simpleSwapTabuList.Add(m1);
            }
            if(c2.CourseID!=null){
            simpleMove m2 = new simpleMove();
            m2.courseId = c2.CourseID;
            m2.t = swap[0];
            m2.r = swap[1];
            X.setNr(c2.CourseID);
                X.setNd(c2Index);
            simpleSwapTabuList.Add(m2);
            }




        }
        public struct simpleMove
        {
            public int r, t;
            public string courseId;
           
        }
        struct kempeSwap 
        {
        
        }

    }//end of class tabuSolver



    struct solution
    {
        /// <summary>
        /// a feasible timetable
        /// </summary>
        public course[][] timeTable;
        /// <summary>
        /// the total number of available periods for course[i] under this solution
        /// </summary>
        public int[] apd;
        /// <summary>
        /// the total number of available room  for course[i] under this solution
        /// </summary>
        public int[] arm;
        /// <summary>
        /// the total number of available positions(periods-time pairs) for course[i] under this solution
        /// </summary>
        public int[] aps;
        /// <summary>
        /// the number of unassigned lectures of course[i] under this solution
        /// </summary>
        public int[] nl;
        /// <summary>
        /// /// uac(i,j) the total number of lectures of unfinished courses who become unavailable
        /// at period j after assigning one lecture of course[i] at period j
        /// </summary>
        /// <param name="i">course index</param>
        /// <param name="j">period index</param>
        /// <returns></returns>
        public int uac(int i, int[] j)
        {
            bool find = false;
            int _uac = 0;
            foreach (var c in LC)
            {
                if (c.Key != i)
                {
                    foreach (var cons in Constraints)
                    {
                        if (c.Value.CourseID == cons.CourseID && j[0] == cons.Day && j[1] == cons.Day_Period)
                        {
                            find = true;
                        }
                    }
                    if (!find)
                    {
                        _uac += c.Value.Lectures - nl[c.Key];
                    }
                }
            }
            return _uac;
        }
        /// <summary>
        /// لیست دروسی که به طور کامل زمانبندی نشده اند
        /// </summary>
        public Dictionary<int, course> LC;
        /// <summary>
        /// لیست زمان های غیر مجاز برای هر درس
        /// </summary>
        public List<UNAVAILABILITY_CONSTRAINT> Constraints;

        /// <summary>
        /// number of room occupied by courses in this solution
        /// </summary>
        public int[] nr;

        /// <summary>
        /// the number of working Days that course i takes place at for this condidate solution
        /// </summary>
        public int[] nd;

        double k1, k2;

        public problemInstance p;

        /// <summary>
        /// whether curriculum CRk appears at period ti for
        /// a candidate solution X
        /// </summary>
        /// <param name="k">Kth curriculum</param>
        /// <param name="i">period</param>
        /// <returns>1 : when any course in curiculum CRk is scheduled at period Ti and return 0 otherwise</returns>
        public int app( int k, int i)
        {

            foreach (var item in p.Curricula[k].Courses)
            {
                foreach (var c in timeTable[i])
                {
                    if (item == c.CourseID) return 1;
                }
            }
            return 0;
        }

        public List<int> getUavPeriods(int cindex)
        {
            List<int> uap = new List<int>();
            course c = LC[cindex];
            foreach (var item in Constraints)
            {
                if (item.CourseID == c.CourseID)
                {
                    uap.Add(item.Day * p.Periods_per_day + item.Day_Period); 
                }
            }
            return uap;
        }


        public void insertLecture(int Ci, int Tj, int Rk)
        {
            this.timeTable[Tj][Rk] = LC[Ci];
            setNr(Ci);
            setApd(Ci,Tj);
            setNd(Ci);
            this.nl[Ci]--;
            if (nl[Ci]==0)
                LC.Remove(Ci);
            //this.
        }
        public void setNd(int Ci)
        {
            string cId = p.courses[Ci].CourseID;
            int d1, d2;
            d1 = d2 = 0;
            for (int i = 0; i < this.timeTable.Length; i++)
            {
                for (int r = 0; r < this.timeTable[0].Length; r++)
                {
                    if (this.timeTable[i][r].CourseID == cId)
                    {
                        if (d1 == 0)
                        {
                            d1 = i / p.Periods_per_day;
                        }
                        else
                        {
                            d2 = i / p.Periods_per_day;
                            nd[Ci] = d2 - d1 + 1;
                            return;
                        }
                    }
                }
            }


        }

        public void setApd(int Ci,int Tj)
        {
          string cID=LC[Ci].CourseID;
          List<string> effectedCourses = new List<string>();
          for (int i = 0; i < p.Curricula.Length; i++)
          {
              foreach (var item in p.Curricula[i].Courses)
              {
                 if(item==cID){
                     foreach (var c in p.Curricula[i].Courses)
                     {
                         if (!effectedCourses.Contains(c))
                         {
                             effectedCourses.Add(c);
                         }
                     }
              } 
              }
              
          }
          for (int i = 0; i < LC.Count; i++)
          {
              for (int j = 0; j < effectedCourses.Count; j++)
			{
                if (LC[i].CourseID == effectedCourses[j])
                {
                    apd[i]--;
                    UNAVAILABILITY_CONSTRAINT constraint = new UNAVAILABILITY_CONSTRAINT();
                    constraint.CourseID = LC[i].CourseID;
                    constraint.Day = Tj / p.Periods_per_day;
                    constraint.Day_Period = Tj % p.Periods_per_day;
                    Constraints.Add(constraint); 
                }
			}
              
          }

        }
        public void setNr(int ci)
        {
            List<int> rooms = new List<int>();
            for (int i = 0; i < timeTable.Length; i++)
            {
                for (int j = 0; j < timeTable[i].Length; j++)
                {
                    if (timeTable[i][j].CourseID == LC[ci].CourseID && !rooms.Contains(j))
                    {
                        rooms.Add(j);
                    }
                }
            }
            nr[ci] = rooms.Count;
        }

        public void setNr(string cid)
        {
            List<int> rooms = new List<int>();
            for (int i = 0; i < timeTable.Length; i++)
            {
                for (int j = 0; j < timeTable[i].Length; j++)
                {
                    if (timeTable[i][j].CourseID == cid && !rooms.Contains(j))
                    {
                        rooms.Add(j);
                    }
                }
            }
            for (int i = 0; i < p.courses.Length; i++)
            {
                if (p.courses[i].CourseID == cid) nr[i] = rooms.Count;
                i = p.courses.Length;
            }
        }


        public void initConstraints()
        {
            Constraints = new List<UNAVAILABILITY_CONSTRAINT>();
            Constraints=p.Constraints.ToList();
        
        }





        public void initSol(problemInstance P)
        {

            this.p = P;
            timeTable = new course[p.Periods_per_day * p.Days][];
            for (int i = 0; i < p.Periods_per_day * p.Days; i++)
            {
                timeTable[i] = new course[p.Rooms.Length];
            }



            LC = new Dictionary<int, course>();
            for (int i = 0; i < p.courses.Length; i++)
            {
                LC.Add(i, p.courses[i]);//Algorithm 1 line 3: init LC
            }
            nl = new int[p.courses.Length];
            nr = new int[p.courses.Length];
            nd = new int[p.courses.Length];
            for (int i = 0; i < p.courses.Length; i++)
            {
                nl[i] = p.courses[i].Lectures;//init nl
                nr[i] = 0;
                nd[i] = 0;
            }

            initApd();
            initArm();
            initAps();
            initConstraints();

        }
        public void initApd()
        {
            apd = new int[p.courses.Length];
            int totalperiod = p.Days * p.Periods_per_day;
            for (int i = 0; i < p.courses.Length; i++)
            {
                apd[i] = totalperiod;
                foreach (var item in p.Constraints)
                {
                    if (item.CourseID == p.courses[i].CourseID)
                        apd[i]--;
                }
            }
        }
        public void initArm()
        {
            arm = new int[p.courses.Length];
            for (int i = 0; i < p.courses.Length; i++)
            {
                arm[i] = p.Rooms.Length;
                foreach (var item in p.Rooms)
                {
                    if (p.courses[i].Students > item.Capacity)
                    {
                        arm[i]--;
                    }
                }
            }
        }
        public void initAps()
        {
            aps = new int[p.courses.Length];
            for (int i = 0; i < p.courses.Length; i++)
            {
                aps[i] = arm[i] * apd[i];
            }
        }
        public void setAps()
        {
            initAps();//felan alaki! badan bayad doros beshe
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <returns>اندیس درسی که باید زمانبندی شود را برمی گرداند</returns>
        public int HR1()
        {
            double hr1 = 0;
            double hrmin = double.MaxValue;
            List<int> cids = new List<int>();
            for (int i = 0; i < apd.Length; i++)
            {
                if (nl[i] > 0)
                {
                    hr1 = apd[i] / Math.Sqrt(nl[i]);
                    if (hr1 < hrmin)
                    {
                        cids.Clear();
                        cids.Add(i);
                    }
                    if (hr1 == hrmin)
                    {
                        cids.Add(i);
                    }
                }
            }
            if (cids.Count == 1) return cids.First();//HR1(i)
            hrmin = double.MaxValue;
            List<int> cids2 = new List<int>();
            foreach (int item in cids)
            {
                hr1 = aps[item] / Math.Sqrt(nl[item]);
                if (hr1 < hrmin)
                {
                    cids2.Clear();
                    cids2.Add(item);
                }
                if (hr1 == hrmin)
                {
                    cids2.Add(item);
                }
            }
            if (cids2.Count == 1) return cids2.First();//HR1(ii)

            cids.Clear();
            int hrmax = -1;
            int cindex = cids2.First();
            foreach (var item in cids2)
            {
                string cid = LC[item].CourseID;

                hr1 = 0;
                foreach (var conf in p.Curricula)
                {
                    foreach (var memConf in conf.Courses)
                    {
                        if (memConf == cid)
                        {
                            hr1++;
                        }
                    }
                }


                if (hr1 > hrmax)
                {
                    hrmax = (int)hr1;
                    cindex = item;
                }

            }
            return cindex;//HR1(iii)

        }
        public int[] HR2(int cindex)
        {
            k1 = 1;
            k2 = 0.5;
            int[] pos = new int[2];
            pos[0] = -1;
            List<int> uavtimes=new List<int>();
            uavtimes = getUavPeriods(cindex);

            double minCost = double.MaxValue;
            int[] bestPos = new int[2];
            
            
            for (int t = 0; t < p.Periods_per_day*p.Days; t++)
            {
                if (!uavtimes.Contains(t))
                {
                    for (int r = 0; r < p.Rooms.Length; r++)
                    {
                        if(timeTable[t][r].CourseID==null){
                        int[] pr = new int[2];
                        pr[0] = t / p.Days;
                        pr[1] = t % p.Periods_per_day;
                        solution tempSol = new solution();
                        solutionCopier.copy(this, out tempSol);
                        tempSol.insertLecture(cindex, t, r);

                        double cost = k1 * uac(cindex, pr) + k2 * validator.getCost(p, tempSol);
                        if (cost < minCost)
                        {
                            minCost = cost;
                            bestPos[0] = t;
                            bestPos[1] = r;
                        }}
                    }
                }  
            }
            return bestPos;
        }

    }

    class problemInstance
    {

        public string Name;
        public int CoursesNum;
        public int RoomsNum;
        public int Days;
        public int Periods_per_day;
        public int CurriculaNum;
        public int ConstraintsNum;
        /// <summary>
        /// لیست تمام دروسی که باید زمانبندی شوند
        /// </summary>
        public course[] courses;
        /// <summary>
        /// لیست تمام کلاس های موجود
        /// </summary>
        public room[] Rooms;
        /// <summary>
        /// لیست برنامه های درسی (دروسی که دانشجویان مشابهی دارند)ا
        /// </summary>
        public Curriculum[] Curricula;
        /// <summary>
        /// لیست زمان های غیر مجاز برای هر درس
        /// </summary>
        public UNAVAILABILITY_CONSTRAINT[] Constraints;
    }
    struct course
    {
        /// <summary>
        /// شناسه درس
        /// </summary>
        public string CourseID;
        /// <summary>
        /// استاد درس
        /// </summary>
        public string Teacher;
        /// <summary>
        /// تعداد جلسات درس
        /// </summary>
        public int Lectures;
        /// <summary>
        /// حداقل فاصله ی زمانی که بین جلسات باید باشد 
        /// </summary>
        public int MinWorkingDays;
        /// <summary>
        /// تعداد دانشجویان این درس
        /// </summary>
        public int Students;
    }

    struct room
    {
        /// <summary>
        /// شناسه کلاس
        /// </summary>
        public string ID;
        /// <summary>
        /// گنجایش کلاس
        /// </summary>
        public int Capacity;
    }

    struct Curriculum
    {
        public string ID;
        /// <summary>
        /// دروسی که در یک برنامه درسی هستند و نباید همزمان برگذار شوند
        /// </summary>
        public string[] Courses;
    }
    struct UNAVAILABILITY_CONSTRAINT
    {
        /// <summary>
        /// شناسه درس
        /// </summary>
        public string CourseID;
        /// <summary>
        /// روز
        /// </summary>
        public int Day;
        /// <summary>
        /// ساعت
        /// </summary>
        public int Day_Period;
    }

    class validator
    {
        /// <summary>
        /// penalties associated to each of soft constraints
        /// </summary>
        public static double alpha1, alpha2, alpha3, alpha4;

        static problemInstance P;

        /// <summary>
        /// solution to validate
        /// </summary>
        static solution X;
        public static double getCost(problemInstance p, solution x)
        {
            alpha1 = 1;
            alpha2 = 1;
            alpha3 = 5;
            alpha4 = 2;
            P = p;
            if (x.apd == null) {
                X = x;
            }
            X = x;
            return f();
        }
        /// <summary>
        /// Room Capasity
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Ri">room i</param>
        /// <param name="Pj">period j</param>
        /// <returns>cost of Room Capasity</returns>
        public static double f1(int Pi, int Rj)
        {
            double cost = 0;
            int std = X.timeTable[Pi][Rj].Students;
            int cap = P.Rooms[Rj].Capacity;
            if (std > cap) cost = alpha1 * (std - cap);
            return cost;
        }
        /// <summary>
        /// Room stability
        /// </summary>
        /// <param name="Ci">course index</param>
        /// <returns>cost of Room stability for course[Ci]</returns>
        public static double f2(int Ci)
        {
            double cost = 0;
            cost = alpha2 * (X.nr[Ci] - 1);
            return cost;
        }
        public static double f3(int Ci)
        {
            double cost = 0;
            if (X.nd[Ci] < P.courses[Ci].MinWorkingDays) cost = alpha3 * (P.courses[Ci].MinWorkingDays - X.nd[Ci]);
            return cost;
        }
        public static double f4(int Pi, int Rj)
        {
            double cost = 0;
            for (int q = 0; q < P.Curricula.Length; q++)
            {
                int c_cr = 0;
                if (P.Curricula[q].Courses.Contains(X.timeTable[Pi][Rj].CourseID)) c_cr = 1;
                cost += c_cr * iso(X, q, Pi);

            }

            return cost * alpha4;

        }
        public static double f()
        {
            double cost = 0;
            double sigma1, sigma2, sigma3, sigma4;
            sigma1 = 0; sigma2 = 0; sigma3 = 0; sigma4 = 0;
            for (int i = 0; i < P.Days * P.Periods_per_day; i++)
            {
                for (int j = 0; j < P.Rooms.Length; j++)
                {
                    sigma1 += f1(i, j);
                    sigma4 += f4(i, j);
                }
            }
            for (int i = 0; i < P.courses.Length; i++)
            {
                sigma2 += f2(i);
                sigma3 += f3(i);
            }

            cost = sigma1 + sigma2 + sigma3 + sigma4;

            return cost;
        }

        public static int iso(solution X,int q, int i)
        {

            if ((i % P.Periods_per_day == 1 && X.app(q, i - 1) == 0) || (i % P.Periods_per_day == 0 && X.app(q, i + 1) == 0))
            {
                return 1;
            }
            return 0;
        }
    }

    class solutionCopier
    {
        /// <summary>
        /// make a deep copy of solution s to solution d
        /// </summary>
        /// <param name="s">source solution</param>
        /// <param name="d">destination solution</param>
        public static void copy(solution s, out solution d)
        {
           
            d = new solution();
            d.p = s.p;
            d.apd = new int[s.apd.Length];

            d.aps = new int[s.aps.Length];
            d.arm = new int[s.arm.Length];
            d.Constraints = new List<UNAVAILABILITY_CONSTRAINT>();
            for (int i = 0; i <s.Constraints.Count; i++)
			{
                UNAVAILABILITY_CONSTRAINT Constraint = new UNAVAILABILITY_CONSTRAINT();
                Constraint.CourseID = s.Constraints[i].CourseID;
                Constraint.Day = s.Constraints[i].Day;
                Constraint.Day_Period = s.Constraints[i].Day_Period;
                d.Constraints.Add(Constraint);
			}
                
            
            d.LC = new Dictionary<int, course>();
            foreach (var item in s.LC)
            {
                d.LC.Add(item.Key, item.Value);// its not a deep copy of s.LC
            }
            d.nd = new int[s.nd.Length];
            d.nl = new int[s.nl.Length];
            d.nr = new int[s.nr.Length];

            for (int i = 0; i < s.nl.Length; i++)
            {
                d.apd[i] = s.apd[i];
                d.aps[i] = s.aps[i];
                d.arm[i] = s.arm[i];
                d.nd[i] = s.nd[i];
                d.nl[i] = s.nl[i];
                d.nr[i] = s.nr[i];
            }


            d.timeTable = new course[s.timeTable.Length][];
            for (int i = 0; i < s.timeTable.Length; i++)
            {
                d.timeTable[i] = new course[s.timeTable[i].Length];
                for (int j = 0; j < s.timeTable[i].Length; j++)
                {
                    d.timeTable[i][j] = s.timeTable[i][j];
                }
            }
            
        }
    }
}

