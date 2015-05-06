// (c) Microsoft Corporation 2005-2009.

namespace Persimmon.Quotations.Evaluator

    open System
    open System.Linq.Expressions
    open System.Collections.Concurrent

    module ExtraHashCompare =
        /// An intrinsic for compiling <c>&lt;@ x <> y @&gt;</c> to expression trees
        val GenericNotEqualIntrinsic : 'T -> 'T -> bool

    [<Sealed>]
    type QuotationEvaluator = 

        /// Compile the quotation expression by first converting to LINQ expression trees
        /// The expression is currently always compiled.
        ///
        /// Exceptions: InvalidArgumentException will be raised if the input expression is
        /// not in the subset that can be converted to a LINQ expression tree
        static member Compile : Microsoft.FSharp.Quotations.Expr<'T>
          -> ConcurrentDictionary<TestCaseTree, obj> * (unit -> 'T)
        
    /// This module provides Compile extension members
    /// for F# quotation values, implemented by translating to LINQ
    /// expression trees and using the LINQ dynamic compiler.
    [<AutoOpen>]
    module QuotationEvaluationExtensions =

        type Microsoft.FSharp.Quotations.Expr<'T> with

              /// Compile and evaluate the quotation expression by first converting to LINQ expression trees.
              /// The expression is currently always compiled.
              ///
              /// Exceptions: InvalidArgumentException will be raised if the input expression is
              /// not in the subset that can be converted to a LINQ expression tree
              member Compile : unit -> ConcurrentDictionary<TestCaseTree, obj> * (unit -> 'T)

    module QuotationEvaluationTypes =

        /// This function should not be called directly. 
        //
        // NOTE: when an F# expression tree is converted to a Linq expression tree using ToLinqExpression 
        // the transformation of <c>LinqExpressionHelper(e)</c> is simple the same as the transformation of
        // 'e'. This allows LinqExpressionHelper to be used as a marker to satisfy the C# design where 
        // certain expression trees are constructed using methods with a signature that expects an
        // expression tree of type <c>Expression<T></c> but are passed an expression tree of type T.
        val LinqExpressionHelper : 'T -> Expression<'T>
