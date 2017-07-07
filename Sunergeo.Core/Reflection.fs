namespace Sunergeo.Core
module Reflection =

    open System.Linq.Expressions
    open System.Reflection

    type ObjectActivator<'a> = delegate of obj[] -> 'a

    let getActivator<'a>
        (ctor: ConstructorInfo)
        (ctorParameters: ParameterInfo[])
        : ObjectActivator<'a> =

        let param = Expression.Parameter(typeof<obj[]>, "args")

        let argsExpression: Expression[] = 
            ctorParameters
            |> Array.mapi
                (fun index parameterInfo ->
                    let index = Expression.Constant(index) :> Expression
                    let paramType = parameterInfo.ParameterType
                    let paramAccessorExp = Expression.ArrayIndex(param, index)
                    upcast Expression.Convert(paramAccessorExp, paramType)
                )

        let newExpression = Expression.New(ctor, argsExpression)
        let lambda = Expression.Lambda(typeof<ObjectActivator<'a>>, newExpression, param)
        lambda.Compile() :?> ObjectActivator<'a>