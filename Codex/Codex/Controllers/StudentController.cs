﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.IO;
using System.Web;
using System.Web.Mvc;
using Codex.Services;
using Codex.Models;

/*using Codex.Models.StudentModels.HelperModels;
using Codex.Models.StudentModels.ViewModels;*/

namespace Codex.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly UserService _userService;
        private readonly FileService _fileService;
        private readonly StudentService _studentService;

        public StudentController() {
            _userService = new UserService();
            _fileService = new FileService();
            _studentService = new StudentService();
        }


        // GET: Student
        public ActionResult Index() {
            var studentId = _userService.GetUserIdByName(User.Identity.Name);

            // Populate assignments
            var userAssignments = _studentService.GetStudentAssignmentsByStudentId(studentId);

            foreach (var assignment in userAssignments) {
                assignment.Problems = _studentService.GetStudentProblemsByAssignmentId(assignment.Id);

                foreach (var problem in assignment.Problems) {
                    problem.Submissions = _studentService.GetSubmissionsByAssignmentGroup(studentId, problem.Id, assignment.Id);
                    problem.IsAccepted = _studentService.IsProblemDone(problem);
                    problem.BestSubmission = _studentService.GetBestSubmission(problem.Submissions);
                }

                assignment.TimeRemaining = _studentService.GetAssignmentTimeRemaining(assignment);
                assignment.IsDone = _studentService.IsAssignmentDone(assignment);
                assignment.NumberOfProblems = assignment.Problems.Count + " " + (assignment.Problems.Count == 1 ? "problem" : "problems");
            }

            StudentViewModel model = new StudentViewModel {
                Assignments = userAssignments
            };

            ViewBag.UserName = User.Identity.Name;
            return View(model);
        }

        public ActionResult Submit(HttpPostedFileBase file, int? assignmentId, int? problemId) {
            if (file != null && 0 < file.ContentLength && assignmentId.HasValue && problemId.HasValue) {
                var userId = _userService.GetUserIdByName(User.Identity.Name);

                var submissionId = _studentService.InsertSubmissionToDatabase(file, assignmentId.Value, problemId.Value, userId);

                if (submissionId != 0) {
                    if (_fileService.UploadSubmissionToServer(file, assignmentId.Value, problemId.Value, submissionId)) {
                        if (_fileService.CompileCPlusPlusBySubmissionId(submissionId)) {
                            if (_fileService.RunTestCasesBySubmissionId(submissionId)) {
                                return Json("success");
                            }
                            else {
                                return Json("case");
                            }
                        }
                        else {
                            return Json("compile");
                        }
                    }
                    else {
                        return Json("write");
                    }
                }
                else {
                    return Json("db");
                }
            }

            return Json(false);
        }
    }
}