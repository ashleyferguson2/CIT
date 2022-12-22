using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.OleDb;
using System.Drawing;
using Excel = Microsoft.Office.Interop.Excel;
using static System.Net.WebRequestMethods;
using System.Xml.Linq;
using System.Configuration;
using System.Diagnostics;




namespace ClaimsInquiryTool
{


    /*SECTION A*/

    /* a1 - declare variables
       a2 - data conection: RPT
       a3 - form load
    */


    //a1
    public partial class frm_Claims_Inquiry_Tool : Form
    {
        string grgr_id_user_entered;
        string prpr_npi_user_entered;
        string sbsb_id_user_entered;
        string sbsb_DOB_user_entered;
        string sbsb_suffix_user_entered;
        string date_type_selected;
        string full_query_text;


        bool prpr_npi_checkboxes_validated;
        bool status_checkboxes_validated;
        bool subtype_checkboxes_validated;
        bool lob_checkboxes_validated;


        bool sbsb_id_user_entered_validated;
        bool grgr_id_user_entered_validated;
        bool prpr_id_user_entered_validated;
        bool npi_user_entered_validated;
        bool all_user_selections_validated;
        bool prpr_npi_txt_bx_validated;
        bool ip_op_checkboxes_validated;


        string sbsb_id_exists_query_text;
        string prpr_npi_id_exists_query_text;
        string grgr_id_exists_query_text;


        string temp_tbl_where_clause;
        string temp_tbl_select_from_clauses;
        string temp_tbl_compl_query_text;
        bool temp_tbl_needed;


        string main_query_select_clause;
        string main_query_from_clause;
        string main_query_where_clause;
        string main_query_subtype_where_subclause;
        string main_query_status_where_subclause;
        string query_text_unselected_lobs;


        string main_query_order_clause;
        string main_query_compl_query_text;


        string begin_dt;
        string end_dt;





        //a2 
        public DataGridView dataGridView1 = new DataGridView();
        static string ConStr = "Data Source=FASQLRPT;Initial Catalog=rpt;Integrated Security=True";
        SqlConnection con = new SqlConnection(ConStr);


        //a3 
        public void ClaimsInquiryTool_Load(object sender, EventArgs e)
        {
            cmbox_date_type.SelectedIndex = 0;

            dtp_begin_dt.MinDate = DateTime.Today.AddDays(-735);
            dtp_begin_dt.MaxDate = DateTime.Today;
            dtp_begin_dt.Value = DateTime.Today.AddDays(-735);


            dtp_end_dt.MinDate = DateTime.Today.AddDays(-735);
            dtp_end_dt.MaxDate = DateTime.Today;
            dtp_end_dt.Value = DateTime.Today;



            txb_sbsb_id.Text = null;
            txb_prpr_npi_id.Text = null;
            txb_grgr_id.Text = null;
        }








        /*SECTION B: QUERY FUNCTIONS */


        /* b1 - pull user selected dates and assign to variables for later use. Also pull date type if user wants to query on received or paid date.
           b2 - this function will check user made selections and 1) determine if temp table 
                is needed; 2) create where clause text for this temp table
           b3 - if function b2 returns true for 'temp_tbl_needed', then the temp table text will be 
                created using the where clause constructed by function b2. If b2 returns false for 
                temp_tbl_needed, then the temp table text will renain blank. 
           b4 - create select clause for main query
           b5 - create from clause for main query

                 //Create "sub" WHERE clauses as needed

           b6 - creates statements concerning user lob selections for the where clause
                  > Fully Insured is a bit trickey--currently we are identifing these claims based on the claim info 
                    NOT fitting the criteria to placed into any other lob. 
                    into another lob
                  > if user selects fully insured, but deslects another lob(s), we will add statement(s) to exclude the non-selected lob(s) 
                  > if user does not select fully insured, we will add statement(s) to include the selected lob(s) 
           b7 - creates statements concerning user claim subtype selections for the where clause
           b8 - creates statements concerning user claim status selections for the where clause
                 //


           b9 - create where clause for main query. If temp table is needed, and line here so that main query
                will only pull from claims identified in the temp table
     
           b10 - create order by clause
           b11 - assemble the query text

         */


        //b1
        public void fnc_get_dates()
        {

            date_type_selected = "CLCL.CLCL_LOW_SVC_DT";

            if (cmbox_date_type.SelectedIndex == 1)
            { date_type_selected = "CLCL.CLCL_RECD_DT"; }
            else if (cmbox_date_type.SelectedIndex == 2)
            { date_type_selected = "CLCL.CLCL_PAID_DT"; }



            begin_dt = dtp_begin_dt.Value.ToString("MM/dd/yyyy");
            end_dt = dtp_end_dt.Value.ToString("MM/dd/yyyy");
        }


        //b2
        public void fnc_create_temp_tbl_where_clause()
        {

            temp_tbl_needed = false;

            if (!string.IsNullOrEmpty(txb_sbsb_id.Text))
            {


                sbsb_id_user_entered = txb_sbsb_id.Text;
                temp_tbl_where_clause = temp_tbl_where_clause + "AND SBSB.SBSB_ID = '" + sbsb_id_user_entered + "' ";
                temp_tbl_needed = true;
            }


            if (!string.IsNullOrEmpty(txt_DOB.Text))
            {
                sbsb_DOB_user_entered = txt_DOB.Text;
                temp_tbl_where_clause = temp_tbl_where_clause + "AND MEME.MEME_BIRTH_DT = '" + sbsb_DOB_user_entered + "' ";
                temp_tbl_needed = true;
            }


            if (!string.IsNullOrEmpty(txt_suffix.Text))
            {
                sbsb_suffix_user_entered = txt_suffix.Text;
                temp_tbl_where_clause = temp_tbl_where_clause + "AND MEME.MEME_SFX = '" + sbsb_suffix_user_entered + "' ";
                temp_tbl_needed = true;
            }


            if (!string.IsNullOrEmpty(txb_grgr_id.Text))
            {
                grgr_id_user_entered = txb_grgr_id.Text;
                temp_tbl_where_clause = temp_tbl_where_clause + "AND GRGR.GRGR_ID = '" + grgr_id_user_entered + "' ";
                temp_tbl_needed = true;
            }


            if (cb_prpr_id.Checked)
            {
                prpr_npi_user_entered = txb_prpr_npi_id.Text;
                temp_tbl_where_clause = temp_tbl_where_clause + "AND CLCL.PRPR_ID = '" + prpr_npi_user_entered + "' ";
                temp_tbl_needed = true;
            }

            if (cb_npi_id.Checked)
            {
                prpr_npi_user_entered = txb_prpr_npi_id.Text;
                temp_tbl_where_clause = temp_tbl_where_clause + "AND PRPR.PRPR_NPI = '" + prpr_npi_user_entered + "' ";
                temp_tbl_needed = true;
            }

            if ((cb_setting_ip.Checked && !cb_setting_op.Checked) || (!cb_setting_ip.Checked && cb_setting_op.Checked))
            {

                temp_tbl_needed = true;

                if (cb_setting_ip.Checked)
                {
                    temp_tbl_where_clause = temp_tbl_where_clause + "AND CDML.CDML_POS_IND = 'I' ";
                }
                else
                    temp_tbl_where_clause = temp_tbl_where_clause + "AND CDML.CDML_POS_IND = 'O' ";
            }

            if (!cb_ITS_Home.Checked)
            {
                temp_tbl_where_clause = temp_tbl_where_clause + "AND CLCL.CLCL_PRE_PRICE_IND NOT IN ('H','T') ";

            }



        }


        //b3
        public void fnc_create_temp_tbl()

        {
            temp_tbl_select_from_clauses = "";
            temp_tbl_where_clause = "";

            fnc_create_temp_tbl_where_clause();

            if (temp_tbl_needed)
            {

                temp_tbl_select_from_clauses = @" 
                               DROP TABLE IF EXISTS ##temp_tbl;
                               SELECT DISTINCT CLCL.CLCL_ID 
                               INTO ##temp_tbl
                               FROM CMC_CLCL_CLAIM CLCL 
                                   INNER JOIN CMC_PRPR_PROV PRPR
                                     ON CLCL.PRPR_ID = PRPR.PRPR_ID 
                                   INNER JOIN CMC_GRGR_GROUP GRGR 
                                     ON CLCL.GRGR_CK = GRGR.GRGR_CK 
                                   INNER JOIN CMC_CDML_CL_LINE CDML
                                     ON CDML.CLCL_ID = CLCL.CLCL_ID 
                                   INNER JOIN CMC_MEME_MEMBER MEME 
                                     ON CLCL.MEME_CK = MEME.MEME_CK
                                   INNER JOIN CMC_SBSB_SUBSC SBSB 
                                     ON CLCL.SBSB_CK = SBSB.SBSB_CK ";


                temp_tbl_where_clause = "WHERE " + date_type_selected + " BETWEEN '" + begin_dt + "' AND '" + end_dt + "' " + temp_tbl_where_clause;
            }

            temp_tbl_compl_query_text = temp_tbl_select_from_clauses + temp_tbl_where_clause;
        }

        //b4
        public void fnc_main_query_select_clause()
        {
            main_query_select_clause =
                 @"
                  DROP TABLE IF EXISTS  ##tmp_initialpull; 
                  SELECT DISTINCT 
                  CLCL.CLCL_ID [Claim No]
                , CDML.CDML_SEQ_NO [Claim Line No]
                , GRGR.GRGR_ID [Group ID]
                , CLCL.PDPD_ID [Product ID]
                , SBSB.SBSB_ID [Subscriber ID]
                , MEME.MEME_SFX [Member Suffix]
                , MEME.MEME_LAST_NAME [Member Last Name]
                , MEME.MEME_FIRST_NAME [Member First Name]
                , CLCL.CLCL_CL_SUB_TYPE [Claim Subtype]
                , CLCL.CLCL_PRE_PRICE_IND [Pre - Priced Ind]
                , CLCL.CLCL_NTWK_IND [Network Ind]
                , CDML.CDML_CUR_STS [Claim Status]
                , CLCL.CLST_MCTR_REAS [Reason Code]
                , CDML.LOBD_ID [LOB ID]
                , CASE
	                    WHEN CLCL_PRE_PRICE_IND IN ('E','S')
	                         THEN 'ITS Host'
                        WHEN GRGR.GRGR_CK IN ('41834','41837','42035','41847','43107')
	                         THEN 'CLTS' 
	                    WHEN GRGR.GRGR_CK = '20147'
	                         THEN 'OGB'
	                    WHEN GRGR.GRGR_CK = '19935'
	                         THEN 'FEP'
	                    WHEN LEFT(CLCL.PDPD_ID,1) IN ('C','A','B','D','N')
	                         THEN 'Individual'
	                    WHEN GRGR.GRGR_MCTR_TYPE = 'BBS'
	                         THEN 'BBS'
	                    WHEN MCRE_GRRE_ID LIKE '2%'
	                         THEN 'SF/ASO Group'
	                    ELSE 'FI/Regular Group'
	              END AS [Area]
                , CONVERT(VarChar(25), CDML.CDML_FROM_DT, 101)  [Srv From Date]
                , CONVERT(VarChar(25), CDML.CDML_TO_DT, 101) [Srv To Date]
                , CONVERT(VarChar(25), CLCL.CLCL_PAID_DT, 101)  [Paid Date]
                , CONVERT(VarChar(25), CLCL.CLCL_RECD_DT, 101)  [Received Date]
                , CAST (CDML.CDML_CHG_AMT AS DECIMAL(20,2)) [Amount Charged]
                , CAST (CDML.CDML_CONSIDER_CHG AS DECIMAL(20,2)) [Considered Charge]
                , CAST (CDML.CDML_ALLOW AS DECIMAL(20,2)) [Allowable Amount]
                , CAST (CDML.CDML_DED_AMT AS DECIMAL(20,2)) [Deductible Amount]
                , CAST (CDML.CDML_COINS_AMT  AS DECIMAL(20,2)) [Coinsurance Amount]
                , CAST (CDML.CDML_COPAY_AMT  AS DECIMAL(20,2)) [Copay Amount]
                , CAST (CDML.CDML_PAID_AMT  AS DECIMAL(20,2)) [Paid Amount]
                , CAST (CDML.CDML_DISALL_AMT  AS DECIMAL(20,2)) [Disallow Amount]
                , CDML.CDML_DISALL_EXCD [Disallow Expl Code]
                , CDML.PSCD_ID [Place Of Srv]
                , CDML.IPCD_ID [Procedure Code]
                , CDML.IDCD_ID [Diagnosis Code]
                , CDML.RCRC_ID [Revenue Code]
                , CDML.SESE_ID [Service ID]
                , CDML.SESE_RULE [Service Rule]
                , CDML.PRPR_ID [Provider ID]
                , PRPR.PRPR_NPI [Provider NPI]
                , PRPR.PRPR_NAME [Provider Name]
                , PRPR.MCTN_ID [Provider TAXID]
                , PRPR.PRPR_MCTR_TYPE [Provider Type]
                , PRPR.PRCF_MCTR_SPEC [Provider Specialty] 
                  INTO ##tmp_initialpull ";
        }

        //b5
        public void fnc_main_query_from_clause()
        {
            main_query_from_clause = @" FROM
                CMC_SBSB_SUBSC SBSB 
                INNER JOIN CMC_GRGR_GROUP GRGR ON SBSB.GRGR_CK = GRGR.GRGR_CK 
                INNER JOIN CMC_MEME_MEMBER MEME ON SBSB.SBSB_CK = MEME.SBSB_CK 
                INNER JOIN CMC_CLCL_CLAIM CLCL ON (SBSB.SBSB_CK = CLCL.SBSB_CK AND MEME.MEME_CK = CLCL.MEME_CK)
                INNER JOIN CMC_CDML_CL_LINE CDML ON CLCL.CLCL_ID = CDML.CLCL_ID 
                LEFT  JOIN CMC_GRRE_RELATION GRRE ON GRRE.GRGR_CK = GRGR.GRGR_CK 
                LEFT  OUTER JOIN CMC_PRPR_PROV PRPR ON CLCL.PRPR_ID = PRPR.PRPR_ID ";
        }


        //b6


        //b7
        public void fnc_main_query_subtype_where_subclause()
        {
            main_query_subtype_where_subclause = "";

            if (!cb_subtype_prof.Checked)
            { main_query_subtype_where_subclause = main_query_subtype_where_subclause + " AND CLCL.CLCL_CL_SUB_TYPE != 'M' "; }

            if (!cb_subtype_fac.Checked)
            { main_query_subtype_where_subclause = main_query_subtype_where_subclause + " AND CLCL.CLCL_CL_SUB_TYPE != 'H'"; }
        }

        //b8
        public void fnc_main_query_status_where_subclause()
        {
            main_query_status_where_subclause = "";

            if (!cb_status_01.Checked)
            {
                main_query_status_where_subclause = main_query_status_where_subclause +
                         " AND CDML.CDML_CUR_STS != '01' ";
            }

            if (!cb_status_02.Checked)
            {
                main_query_status_where_subclause = main_query_status_where_subclause +
                         " AND CDML.CDML_CUR_STS != '02' ";
            }
            if (!cb_status_11.Checked)
            {
                main_query_status_where_subclause = main_query_status_where_subclause +
                         " AND CDML.CDML_CUR_STS != '11' ";
            }
            if (!cb_status_15.Checked)
            {
                main_query_status_where_subclause = main_query_status_where_subclause +
                         " AND CDML.CDML_CUR_STS != '15' ";
            }
            if (!cb_status_91.Checked)
            {
                main_query_status_where_subclause = main_query_status_where_subclause +
                         " AND CDML.CDML_CUR_STS != '91' ";
            }
        }


        //b9
        public void fnc_main_query_where_clause()
        {
            fnc_main_query_subtype_where_subclause();
            fnc_main_query_status_where_subclause();
            main_query_where_clause =
                   "WHERE " + date_type_selected + " BETWEEN '" + begin_dt + "' AND '" + end_dt + "' "

                  + main_query_subtype_where_subclause
                  + main_query_status_where_subclause;

            if (temp_tbl_needed)
            {
                main_query_where_clause = main_query_where_clause + @" AND CLCL.CLCL_ID IN (SELECT * FROM ##temp_tbl) ";
            }
        }


        //b10
        public void fnc_main_query_order_by_clause()
        {
            main_query_order_clause = @"ORDER BY CLCL.CLCL_ID
                                        , CDML.CDML_SEQ_NO ";
        }



        //b11
        public void fnc_main_query_compl_query_text()
        {
            fnc_create_temp_tbl();
            fnc_main_query_select_clause();
            fnc_main_query_where_clause();
            fnc_main_query_from_clause();
            fnc_main_query_order_by_clause();



            main_query_compl_query_text =
                                    temp_tbl_compl_query_text
                                  + main_query_select_clause
                                  + main_query_from_clause
                                  + main_query_where_clause
                                  + main_query_order_clause;

            fnc_full_query_text();
        }





        public void fnc_full_query_text()
        {
            query_text_unselected_lobs = "";
            full_query_text = main_query_compl_query_text +
                 " SELECT * FROM ##tmp_initialpull  ";

            int lob_counter = 0;

            if (!cb_lob_fully.Checked)
            { lob_counter = lob_counter + 1; }

            if (!cb_lob_ogb.Checked)
            { lob_counter = lob_counter + 1; }

            if (!cb_lob_CLTS.Checked)
            { lob_counter = lob_counter + 1; }

            if (!cb_lob_aso.Checked)
            { lob_counter = lob_counter + 1; }

            if (!cb_lob_itshost.Checked)
            { lob_counter = lob_counter + 1; }

            if (!cb_lob_bbs.Checked)
            { lob_counter = lob_counter + 1; }

            if (!cb_lob_indiv.Checked)
            { lob_counter = lob_counter + 1; }

            if (!cb_lob_fep.Checked)
            { lob_counter = lob_counter + 1; }


            if (lob_counter > 1)
            {



                query_text_unselected_lobs = "DROP TABLE IF EXISTS ##unselected_lobs;  CREATE TABLE ##unselected_lobs (Area VARCHAR(25)) ";

                if (!cb_lob_fully.Checked)
                { query_text_unselected_lobs = query_text_unselected_lobs + " INSERT INTO ##unselected_lobs (Area) values ('FI/Regular Group') "; }

                if (!cb_lob_ogb.Checked)
                { query_text_unselected_lobs = query_text_unselected_lobs + " INSERT INTO ##unselected_lobs (Area) values ('OGB') "; }

                if (!cb_lob_CLTS.Checked)
                { query_text_unselected_lobs = query_text_unselected_lobs + " INSERT INTO ##unselected_lobs (Area) values ('CLTS') "; }

                if (!cb_lob_aso.Checked)
                { query_text_unselected_lobs = query_text_unselected_lobs + " INSERT INTO ##unselected_lobs (Area) values ('SF/ASO Group') "; }

                if (!cb_lob_itshost.Checked)
                { query_text_unselected_lobs = query_text_unselected_lobs + " INSERT INTO ##unselected_lobs (Area) values ('ITS Host') "; }

                if (!cb_lob_bbs.Checked)
                { query_text_unselected_lobs = query_text_unselected_lobs + " INSERT INTO ##unselected_lobs (Area) values ('BBS') "; }

                if (!cb_lob_indiv.Checked)
                { query_text_unselected_lobs = query_text_unselected_lobs + " INSERT INTO ##unselected_lobs (Area) values ('Individual') "; }

                if (!cb_lob_fep.Checked)
                { query_text_unselected_lobs = query_text_unselected_lobs + " INSERT INTO ##unselected_lobs (Area) values ('FEP') "; }




                full_query_text = query_text_unselected_lobs + full_query_text;

                full_query_text = full_query_text + " WHERE [Area] NOT IN (SELECT * FROM ##unselected_lobs) ;";



            }




        }





        /*SECTION C: USER ERROR CHECK FUNCTIONS */

        /*
        
           c1 -  If subscriber id box has entry, run query to see if id is valid
                     
        /

           c2 - Do not allow user to mark both the PRPR and NPI checkboxes

           c3 - [c2 true] check to see if user entered text into prpr/npi text box. 
                If text enetered, checkbox for prpr/npi id must also been selected.
        
           c4 - [c3 true] If prpr box is selected, run query to see if id is valid

           c5 - [c4 true] If npi box is selected, run query to see if id is valid

        /

           c6 -  If group id has entry, run query to see if id is valid

        /

           c7 -  Do not allow user to deselect all checkboxes under claim status
           c8 -  Do not allow user to deselect all checkboxes under claim subtype
           c9 -  Do not allow user to deselect all checkboxes under lob
           c10 -  Do not allow user to deselect all checkboxes under claim setting (inpatient/outpatient)

           cz - master validation - calls all other validation functions and sets all_user_selections_validated = true if
                all  other validations pass. Also triggers error messages.

        */




        //c1
        public void fnc_validate_contract_id()
        {

            if (!string.IsNullOrEmpty(txb_sbsb_id.Text))
            {
                sbsb_id_user_entered = txb_sbsb_id.Text;
                sbsb_id_exists_query_text = @"SELECT COUNT(DISTINCT SBSB.SBSB_ID)
                                              FROM CMC_SBSB_SUBSC SBSB
                                              WHERE SBSB.SBSB_ID = '" + sbsb_id_user_entered + "'";

                using (SqlConnection conn = new SqlConnection(ConStr))
                {

                    if (conn.State != ConnectionState.Open)
                    {
                        conn.Close();
                        conn.Open();
                    }

                    SqlCommand cmd = new SqlCommand(sbsb_id_exists_query_text, conn);


                    int sbsb_exists = (int)cmd.ExecuteScalar();

                    if (sbsb_exists == 0)
                    {
                        sbsb_id_user_entered_validated = false;
                        MessageBox.Show("Enter a valid contract number.");
                    }
                    else { sbsb_id_user_entered_validated = true; }
                }
            }
            else
            { sbsb_id_user_entered_validated = true; }


        }

        //c2
        public void fnc_validate_prpr_npi_checkboxes()
        {

            if (cb_prpr_id.Checked && cb_npi_id.Checked)
            {
                prpr_npi_checkboxes_validated = false;
                MessageBox.Show("Please check box for either Provider ID or NPI, or uncheck both.");
            }
            else if (cb_prpr_id.Checked && String.IsNullOrEmpty(txb_prpr_npi_id.Text))
            {
                prpr_npi_checkboxes_validated = false;
                MessageBox.Show("Please either enter a provider id and check the provider id selection box, or delete entered provider id and uncheck the box.");

            }
            else if (cb_npi_id.Checked && String.IsNullOrEmpty(txb_prpr_npi_id.Text))
            {
                prpr_npi_checkboxes_validated = false;
                MessageBox.Show("Please either enter a NPI and check the NPI selection box, or delete entered NPI and uncheck the box.");
            }

            else { prpr_npi_checkboxes_validated = true; }
        }

        //c3
        public void fnc_validate_prpr_npi_txt_bx()
        {
            if (prpr_npi_checkboxes_validated)
            {
                if (!String.IsNullOrEmpty(txb_prpr_npi_id.Text))
                {
                    if (!cb_prpr_id.Checked && !cb_npi_id.Checked)
                    {
                        prpr_npi_txt_bx_validated = false;
                        MessageBox.Show("Please mark either the Provider ID or NPI checkbox.");
                    }
                    else { prpr_npi_txt_bx_validated = true; }
                }
                else { prpr_npi_txt_bx_validated = true; }
            }
        }

        //c4
        public void fnc_validate_prpr_id()
        {
            if (prpr_npi_txt_bx_validated)
            {
                if (cb_prpr_id.Checked)
                {
                    prpr_npi_user_entered = txb_prpr_npi_id.Text;
                    prpr_npi_id_exists_query_text = @"SELECT COUNT(DISTINCT PRPR.PRPR_ID)
                                              FROM CMC_PRPR_PROV PRPR
                                              WHERE PRPR.PRPR_ID = '" + prpr_npi_user_entered + "'";


                    using (SqlConnection conn = new SqlConnection(ConStr))
                    {

                        if (conn.State != ConnectionState.Open)
                        {
                            conn.Close();
                            conn.Open();
                        }

                        SqlCommand cmd = new SqlCommand(prpr_npi_id_exists_query_text, conn);
                        int prpr_exists = (int)cmd.ExecuteScalar();

                        if (prpr_exists == 0)
                        {
                            prpr_id_user_entered_validated = false;
                            MessageBox.Show("Please enter a valid provider id.");
                        }
                        else { prpr_id_user_entered_validated = true; }
                    }
                }
                else { prpr_id_user_entered_validated = true; }
            }
        }

        //c5
        public void fnc_validate_npi_id()
        {

            if (prpr_id_user_entered_validated)
            {

                if (cb_npi_id.Checked)
                {

                    prpr_npi_user_entered = txb_prpr_npi_id.Text;
                    prpr_npi_id_exists_query_text = @"SELECT COUNT(DISTINCT PRPR.PRPR_NPI)
                                              FROM CMC_PRPR_PROV PRPR
                                              WHERE PRPR.PRPR_NPI = '" + prpr_npi_user_entered + "'";


                    using (SqlConnection conn = new SqlConnection(ConStr))
                    {

                        if (conn.State != ConnectionState.Open)
                        {
                            conn.Close();
                            conn.Open();
                        }

                        SqlCommand cmd = new SqlCommand(prpr_npi_id_exists_query_text, conn);
                        int npi_exists = (int)cmd.ExecuteScalar();

                        if (npi_exists == 0)
                        {
                            npi_user_entered_validated = false;
                            MessageBox.Show("Please enter a valid NPI.");
                        }
                        else { npi_user_entered_validated = true; }
                    }
                }

                else { npi_user_entered_validated = true; }

            }
        }

        //c6
        public void fnc_validate_grgr_id()
        {
            if (!String.IsNullOrEmpty(txb_grgr_id.Text))
            {
                grgr_id_user_entered = txb_grgr_id.Text;
                grgr_id_exists_query_text = @"SELECT COUNT(DISTINCT GRGR.GRGR_ID)
                                              FROM CMC_GRGR_GROUP GRGR
                                              WHERE GRGR.GRGR_ID = '" + grgr_id_user_entered + "'";


                using (SqlConnection conn = new SqlConnection(ConStr))
                {

                    if (conn.State != ConnectionState.Open)
                    {
                        conn.Close();
                        conn.Open();
                    }


                    SqlCommand cmd = new SqlCommand(grgr_id_exists_query_text, conn);
                    int grgr_id_exists = (int)cmd.ExecuteScalar();

                    if (grgr_id_exists == 0)
                    {
                        grgr_id_user_entered_validated = false;
                        MessageBox.Show("Please enter a valid group id.");
                    }
                    else { grgr_id_user_entered_validated = true; }
                }
            }
            else { grgr_id_user_entered_validated = true; }
        }


        //c7
        public void fnc_validate_claim_status_checkboxes()
        {

            if (!cb_status_01.Checked
                 && !cb_status_02.Checked
                 && !cb_status_11.Checked
                 && !cb_status_15.Checked
                 && !cb_status_91.Checked
                 )
            {
                status_checkboxes_validated = false;
                MessageBox.Show("At least one claim status selection must be checked.");
            }
            else { status_checkboxes_validated = true; }
        }


        //c8
        public void fnc_validate_claim_subtype_checkboxes()
        {

            if (!cb_subtype_prof.Checked && !cb_subtype_fac.Checked)

            {
                subtype_checkboxes_validated = false;
                MessageBox.Show("At least one claim type (Medical or Professional) selection must be checked.");
            }
            else { subtype_checkboxes_validated = true; }
        }

        //c9
        public void fnc_validate_lob_checkboxes()
        {

            if (!cb_lob_aso.Checked
                 && !cb_lob_fully.Checked
                 && !cb_lob_CLTS.Checked
                 && !cb_lob_itshost.Checked
                 && !cb_lob_ogb.Checked
                 && !cb_lob_bbs.Checked
                 && !cb_lob_indiv.Checked
                 && !cb_lob_fep.Checked
                 )
            {
                lob_checkboxes_validated = false;
                MessageBox.Show("At least one LOB selection must be checked.");
            }
            else { lob_checkboxes_validated = true; }
        }

        //c10
        public void fnc_validate_ip_op_checkboxes()
        {
            if (!cb_setting_ip.Checked && !cb_setting_op.Checked)
            {
                ip_op_checkboxes_validated = false;
                MessageBox.Show("At least one setting category (in-patient or outpatient) must be checked.");
            }
            else
            { ip_op_checkboxes_validated = true; }
        }




        //cz
        public void fnc_master_error_check()
        {
            int pass_counter = 0;


            fnc_validate_contract_id();
            if (sbsb_id_user_entered_validated)
            { pass_counter++; }
            //else MessageBox.Show("Error c1");

            fnc_validate_prpr_npi_checkboxes();
            if (prpr_npi_checkboxes_validated)
            { pass_counter++; }
            //else MessageBox.Show("Error c2");

            fnc_validate_prpr_npi_txt_bx();
            if (prpr_npi_txt_bx_validated)
            { pass_counter++; }
            //else MessageBox.Show("Error c3");

            fnc_validate_prpr_id();
            if (prpr_id_user_entered_validated)
            { pass_counter++; }
            //else MessageBox.Show("Error c4");

            fnc_validate_npi_id();
            if (npi_user_entered_validated)
            { pass_counter++; }
            //else MessageBox.Show("Error c5");


            fnc_validate_grgr_id();
            if (grgr_id_user_entered_validated)
            { pass_counter++; }
            //else MessageBox.Show("Error c6");

            fnc_validate_claim_subtype_checkboxes();
            if (subtype_checkboxes_validated)
            { pass_counter++; }
            //else MessageBox.Show("Error c7");

            fnc_validate_claim_status_checkboxes();
            if (status_checkboxes_validated)
            { pass_counter++; }
            //else MessageBox.Show("Error c8");

            fnc_validate_lob_checkboxes();
            if (lob_checkboxes_validated)
            { pass_counter++; }
            //else MessageBox.Show("Error c9");

            fnc_validate_ip_op_checkboxes();
            if (ip_op_checkboxes_validated)
            { pass_counter++; }
            //else MessageBox.Show("Error c10");




            if (pass_counter == 10)
            { all_user_selections_validated = true; }
            else
            { all_user_selections_validated = false; }

        }









        /*SECTION D: */
        /* When user clicks run button:
            - function fnc_master_error_check() will validate user selctions;
            - if all validations come back clean, functions used to 1) pull dates, 2) create query text, and 3) execute query
              and display the results will trigger. 
        */

        public void btn_Run_Click(object sender, EventArgs e)
        {

            fnc_master_error_check();


            if (all_user_selections_validated == true)
            {
                fnc_get_dates();
                fnc_main_query_compl_query_text();
                fnc_execute_return_results(full_query_text);
            }


        }


        public void fnc_execute_return_results(string full_query_text)
        {


            /*a - using previously defined connection string, create new instance of the connection, called 'conn'
              b - set connection state to open
              c - create new SQL command using our generated query text, and new instance of the connection string
              d - insert the SQL command into a new iinstance of adapter
              e - set time out to 300 seconds (5 min)
              f - create new data table called SQLResulsTable
              g - fill SQLResulsTable with information pulled using our query text
              h - create a new instance of display form frmDisplayQueryResults
              i - update dataGridView1 of the display form to contain results pulled into table SQLResulsTable
              j -  display the new instance of form frmDisplayQueryResults

             */




            //a
            using (SqlConnection conn = new SqlConnection(ConStr))
            {
                //b
                conn.Open();

                //c
                SqlCommand cmd = new SqlCommand(full_query_text, conn);

                //d
                SqlDataAdapter da = new SqlDataAdapter(cmd);

                //e
                da.SelectCommand.CommandTimeout = 10000;

                //f
                DataTable SQLResultsTable = new DataTable();

                //g
                da.Fill(SQLResultsTable);

                //h
                frmDisplayQueryResults frm2 = new frmDisplayQueryResults();

                //i
                frm2.dataGridView1.DataSource = new BindingSource(SQLResultsTable, null);

                //j
                frm2.ShowDialog();


            }
        }










        /*SECTION E: MISC.*/

        public frm_Claims_Inquiry_Tool()
        {
            InitializeComponent();

        }

        public void header_txt(object sender, EventArgs e)
        {

        }

        public void textBox1_TextChanged(object sender, EventArgs e)
        {
        }

        public void txt_Group_ID_TextChanged(object sender, EventArgs e)
        {

        }

        public void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        public void label2_Click(object sender, EventArgs e)
        {

        }

        public void label2_Click_1(object sender, EventArgs e)
        {

        }

        public void label3_Click(object sender, EventArgs e)
        {

        }

        public void txt_Prov_ID_TextChanged(object sender, EventArgs e)
        {

        }

        public void ckb_Setting_IP_CheckedChanged(object sender, EventArgs e)
        {

        }

        public void txt_Setting_TextChanged(object sender, EventArgs e)
        {

        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {

        }

        private void dtp_end_DT_ValueChanged(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void lbl_meme_sbsb_id_Click(object sender, EventArgs e)
        {

        }

        private void panel20_Paint(object sender, PaintEventArgs e)
        {

        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void panel23_Paint(object sender, PaintEventArgs e)
        {

        }

        private void txb_reporting_team2_TextChanged(object sender, EventArgs e)
        {

        }

        private void cb_status_01_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void pnl_menu_claim_subtype_Paint(object sender, PaintEventArgs e)
        {

        }

        private void pnl_menu_claim_subtype_MouseHover(object sender, EventArgs e)
        {

        }

        private void toolTip_claim_status_Popup(object sender, PopupEventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }


        private void toolTip1_Popup(object sender, PopupEventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void lbl_grgr_info_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void cb_lob_fully_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void lblDOB_Click(object sender, EventArgs e)
        {

        }

        private void lbl_ITS_Home_Click(object sender, EventArgs e)
        {

        }

        private void lbl_end_DT_Click(object sender, EventArgs e)
        {

        }
    }
}










