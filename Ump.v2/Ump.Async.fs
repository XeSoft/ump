﻿namespace Ump.Async

open Ump

type Ump<'initArg, 'model, 'effect, 'msg> =
    {
        /// perform is assumed to have it's dependencies partially applied
        perform: 'effect -> Async<'msg>
        logic: Logic<'initArg, 'model, 'effect, 'msg>
    }


module Ump =

    /// perform is assumed to have it's dependencies partially applied
    let create
        (perform: 'effect -> Async<'msg>)
        (logic: Logic<'initArg, 'model, 'effect, 'msg>)
        : Ump<'initArg, 'model, 'effect, 'msg> =
        {
            perform = perform
            logic = logic
        }

    let resume
        (prog: Ump<'initArg, 'model, 'effect, 'msg>)
        (model: 'model)
        (msg: 'msg)
        : Async<'model> =
        async {
            let mutable model = model
            let msgs = System.Collections.Generic.Queue<'msg>(1)
            msgs.Enqueue(msg)
            while msgs.Count > 0 do
                let msg = msgs.Dequeue()
                let model_, effects = prog.logic.update model msg
                model <- model_
                for effect in effects do
                    let! newMsg = prog.perform effect
                    msgs.Enqueue(newMsg)
            return model
        }

    let run
        (prog: Ump<'initArg, 'model, 'effect, 'msg>)
        (initArg: 'initArg)
        : Async<'model> =
        async {
            let model_, msg_ = prog.logic.init initArg
            return! resume prog model_ msg_
        }


