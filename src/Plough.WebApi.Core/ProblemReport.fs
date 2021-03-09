namespace Plough.WebApi

open Plough.ControlFlow

/// RFC 7807 problem format
type ProblemReport =
    { Title : string
      Status : int
      Detail : string }

module ProblemReport =
    let failureTypeAsString failure =
        match failure with
        | FailureMessage.Database _ -> "Database"
        | FailureMessage.Parse _ -> "Parse"
        | FailureMessage.Validation _ -> "Validation"
        | FailureMessage.Conflict _ -> "Data conflict"
        | FailureMessage.Unknown _ -> "Unknown"
        | FailureMessage.NotFound _ -> "Not Found"
        | FailureMessage.ExceptionFailure _ -> "Exception"

    let failureTypeAsHttpStatus failure =
        match failure with
        | FailureMessage.Database _ -> 500
        | FailureMessage.Parse _ -> 400
        | FailureMessage.Validation _ -> 400
        | FailureMessage.Conflict _ -> 409
        | FailureMessage.Unknown _ -> 500
        | FailureMessage.NotFound _ -> 404
        | FailureMessage.ExceptionFailure _ -> 500

    /// Translate a failure message into an RFC 7807 compatible type
    let failureToProblemReport failure : ProblemReport =
        { Title = failureTypeAsString failure
          Status = failureTypeAsHttpStatus failure
          Detail = FailureMessage.unwrap failure }

    /// Translate from our RFC 7807 type into a failure message
    let problemReportToFailure problem : FailureMessage =
        let m = problem.Detail
        match problem.Title with
        | "Database" -> FailureMessage.Database m
        | "Parse" -> FailureMessage.Parse m
        | "Validation" -> FailureMessage.Validation m
        | "Data conflict" -> FailureMessage.Conflict m
        | "Not Found" -> FailureMessage.NotFound m
        | "Exception" -> FailureMessage.ExceptionFailure (exn m)
        | "Unknown" -> FailureMessage.Unknown m
        | _ -> FailureMessage.Unknown m