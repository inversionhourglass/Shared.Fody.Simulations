using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;

namespace Fody.Inspectors
{
    public abstract class MethodBodyVisitor(MethodBody body)
    {
        protected readonly bool _isVoid = body.Method.ReturnType.StrictIsVoid();
        protected readonly MethodBody _body = body;
        protected readonly HashSet<Instruction> _visited = [];
        protected readonly VisitStack _visitStack = new();
        protected MethodStackDepth _stackDepth = new();

        public void Visit()
        {
            if (_body.Instructions.Count == 0) return;

            Visit(_body.Instructions[0]);

            ValidateStackEmpty();

            foreach (var handler in _body.ExceptionHandlers)
            {
                switch (handler.HandlerType)
                {
                    case ExceptionHandlerType.Catch:
                        _stackDepth++;
                        Visit(handler.HandlerStart);
                        break;
                    case ExceptionHandlerType.Filter:
                        _stackDepth++;
                        Visit(handler.FilterStart);
                        ValidateStackEmpty();
                        _stackDepth++;
                        Visit(handler.HandlerStart);
                        break;
                    case ExceptionHandlerType.Finally:
                        Visit(handler.HandlerStart);
                        break;
                    case ExceptionHandlerType.Fault:
                        Visit(handler.HandlerStart);
                        break;
                    default:
                        throw new FodyWeavingException($"Unknow exception handler type: {handler.HandlerType}", handler.HandlerStart, _body.Method);
                }
                ValidateStackEmpty();
            }
        }

        protected virtual void ValidateStackEmpty()
        {
            if (_stackDepth != 0) throw new FodyWeavingException("The stack is not empty after the method returned.", _body.Method);
            _visitStack.Clear();
        }

        protected virtual void Visit(Instruction instruction)
        {
            try
            {
                do
                {
                    var x = _visitStack;
                    if (!_visitStack.TryAdd(instruction))
                    {
                        if (_stackDepth != 0) throw new InvalidOperationException("Stack not empty, let me see see.");
                        return;
                    }
                    if (!_visited.Contains(instruction))
                    {
                        _visited.Add(instruction);
                    }
                    else if (_stackDepth == 0)
                    {
                        return;
                    }

                    switch (instruction.OpCode.Code)
                    {
                        case Code.No:
                            if (VisitNo(instruction)) return;
                            break;
                        case Code.Jmp:
                            if (VisitJmp(instruction)) return;
                            break;
                        case Code.Ret:
                            if (VisitRet(instruction)) return;
                            break;
                        case Code.Endfilter:
                            if (VisitEndfilter(instruction)) return;
                            break;
                        case Code.Throw:
                            if (VisitThrow(instruction)) return;
                            break;
                        case Code.Rethrow:
                            if (VisitRethrow(instruction)) return;
                            break;
                        case Code.Endfinally:
                            if (VisitEndfinally(instruction)) return;
                            break;
                        case Code.Leave:
                            if (VisitLeave(instruction)) return;
                            break;
                        case Code.Leave_S:
                            if (VisitLeave_S(instruction)) return;
                            break;
                        case Code.Br_S:
                            if (VisitBr_S(instruction)) return;
                            break;
                        case Code.Br:
                            if (VisitBr(instruction)) return;
                            break;
                        case Code.Switch:
                            if (VisitSwitch(instruction)) return;
                            break;
                        case Code.Brfalse_S:
                            if (VisitBrfalse_S(instruction)) return;
                            break;
                        case Code.Brtrue_S:
                            if (VisitBrtrue_S(instruction)) return;
                            break;
                        case Code.Brfalse:
                            if (VisitBrfalse(instruction)) return;
                            break;
                        case Code.Brtrue:
                            if (VisitBrtrue(instruction)) return;
                            break;
                        case Code.Beq_S:
                            if (VisitBeq_S(instruction)) return;
                            break;
                        case Code.Bge_S:
                            if (VisitBge_S(instruction)) return;
                            break;
                        case Code.Bgt_S:
                            if (VisitBgt_S(instruction)) return;
                            break;
                        case Code.Ble_S:
                            if (VisitBle_S(instruction)) return;
                            break;
                        case Code.Blt_S:
                            if (VisitBlt_S(instruction)) return;
                            break;
                        case Code.Bne_Un_S:
                            if (VisitBne_Un_S(instruction)) return;
                            break;
                        case Code.Bge_Un_S:
                            if (VisitBge_Un_S(instruction)) return;
                            break;
                        case Code.Bgt_Un_S:
                            if (VisitBgt_Un_S(instruction)) return;
                            break;
                        case Code.Ble_Un_S:
                            if (VisitBle_Un_S(instruction)) return;
                            break;
                        case Code.Blt_Un_S:
                            if (VisitBlt_Un_S(instruction)) return;
                            break;
                        case Code.Beq:
                            if (VisitBeq(instruction)) return;
                            break;
                        case Code.Bge:
                            if (VisitBge(instruction)) return;
                            break;
                        case Code.Bgt:
                            if (VisitBgt(instruction)) return;
                            break;
                        case Code.Ble:
                            if (VisitBle(instruction)) return;
                            break;
                        case Code.Blt:
                            if (VisitBlt(instruction)) return;
                            break;
                        case Code.Bne_Un:
                            if (VisitBne_Un(instruction)) return;
                            break;
                        case Code.Bge_Un:
                            if (VisitBge_Un(instruction)) return;
                            break;
                        case Code.Bgt_Un:
                            if (VisitBgt_Un(instruction)) return;
                            break;
                        case Code.Ble_Un:
                            if (VisitBle_Un(instruction)) return;
                            break;
                        case Code.Blt_Un:
                            if (VisitBlt_Un(instruction)) return;
                            break;
                        case Code.Newobj:
                            if (VisitNewobj(instruction)) return;
                            break;
                        case Code.Call:
                            if (VisitCall(instruction)) return;
                            break;
                        case Code.Callvirt:
                            if (VisitCallvirt(instruction)) return;
                            break;
                        case Code.Calli:
                            if (VisitCalli(instruction)) return;
                            break;
                        case Code.Ldarg:
                            if (VisitLdarg(instruction)) return;
                            break;
                        case Code.Ldarg_S:
                            if (VisitLdarg_S(instruction)) return;
                            break;
                        case Code.Ldarg_0:
                            if (VisitLdarg_0(instruction)) return;
                            break;
                        case Code.Ldarg_1:
                            if (VisitLdarg_1(instruction)) return;
                            break;
                        case Code.Ldarg_2:
                            if (VisitLdarg_2(instruction)) return;
                            break;
                        case Code.Ldarg_3:
                            if (VisitLdarg_3(instruction)) return;
                            break;
                        case Code.Ldarga:
                            if (VisitLdarga(instruction)) return;
                            break;
                        case Code.Ldarga_S:
                            if (VisitLdarga_S(instruction)) return;
                            break;
                        case Code.Ldloca:
                            if (VisitLdloca(instruction)) return;
                            break;
                        case Code.Ldloca_S:
                            if (VisitLdloca_S(instruction)) return;
                            break;
                        case Code.Ldloc:
                            if (VisitLdloc(instruction)) return;
                            break;
                        case Code.Ldloc_S:
                            if (VisitLdloc_S(instruction)) return;
                            break;
                        case Code.Ldloc_0:
                            if (VisitLdloc_0(instruction)) return;
                            break;
                        case Code.Ldloc_1:
                            if (VisitLdloc_1(instruction)) return;
                            break;
                        case Code.Ldloc_2:
                            if (VisitLdloc_2(instruction)) return;
                            break;
                        case Code.Ldloc_3:
                            if (VisitLdloc_3(instruction)) return;
                            break;
                        case Code.Ldc_I4_M1:
                            if (VisitLdc_I4_M1(instruction)) return;
                            break;
                        case Code.Ldc_I4_0:
                            if (VisitLdc_I4_0(instruction)) return;
                            break;
                        case Code.Ldc_I4_1:
                            if (VisitLdc_I4_1(instruction)) return;
                            break;
                        case Code.Ldc_I4_2:
                            if (VisitLdc_I4_2(instruction)) return;
                            break;
                        case Code.Ldc_I4_3:
                            if (VisitLdc_I4_3(instruction)) return;
                            break;
                        case Code.Ldc_I4_4:
                            if (VisitLdc_I4_4(instruction)) return;
                            break;
                        case Code.Ldc_I4_5:
                            if (VisitLdc_I4_5(instruction)) return;
                            break;
                        case Code.Ldc_I4_6:
                            if (VisitLdc_I4_6(instruction)) return;
                            break;
                        case Code.Ldc_I4_7:
                            if (VisitLdc_I4_7(instruction)) return;
                            break;
                        case Code.Ldc_I4_8:
                            if (VisitLdc_I4_8(instruction)) return;
                            break;
                        case Code.Ldc_I4_S:
                            if (VisitLdc_I4_S(instruction)) return;
                            break;
                        case Code.Ldc_I4:
                            if (VisitLdc_I4(instruction)) return;
                            break;
                        case Code.Ldc_I8:
                            if (VisitLdc_I8(instruction)) return;
                            break;
                        case Code.Ldc_R4:
                            if (VisitLdc_R4(instruction)) return;
                            break;
                        case Code.Ldc_R8:
                            if (VisitLdc_R8(instruction)) return;
                            break;
                        case Code.Dup:
                            if (VisitDup(instruction)) return;
                            break;
                        case Code.Ldnull:
                            if (VisitLdnull(instruction)) return;
                            break;
                        case Code.Ldstr:
                            if (VisitLdstr(instruction)) return;
                            break;
                        case Code.Ldsfld:
                            if (VisitLdsfld(instruction)) return;
                            break;
                        case Code.Ldsflda:
                            if (VisitLdsflda(instruction)) return;
                            break;
                        case Code.Ldtoken:
                            if (VisitLdtoken(instruction)) return;
                            break;
                        case Code.Ldftn:
                            if (VisitLdftn(instruction)) return;
                            break;
                        case Code.Arglist:
                            if (VisitArglist(instruction)) return;
                            break;
                        case Code.Ldvirtftn:
                            if (VisitLdvirtftn(instruction)) return;
                            break;
                        case Code.Sizeof:
                            if (VisitSizeof(instruction)) return;
                            break;
                        case Code.Stelem_I:
                            if (VisitStelem_I(instruction)) return;
                            break;
                        case Code.Stelem_I1:
                            if (VisitStelem_I1(instruction)) return;
                            break;
                        case Code.Stelem_I2:
                            if (VisitStelem_I2(instruction)) return;
                            break;
                        case Code.Stelem_I4:
                            if (VisitStelem_I4(instruction)) return;
                            break;
                        case Code.Stelem_I8:
                            if (VisitStelem_I8(instruction)) return;
                            break;
                        case Code.Stelem_R4:
                            if (VisitStelem_R4(instruction)) return;
                            break;
                        case Code.Stelem_R8:
                            if (VisitStelem_R8(instruction)) return;
                            break;
                        case Code.Stelem_Ref:
                            if (VisitStelem_Ref(instruction)) return;
                            break;
                        case Code.Stelem_Any:
                            if (VisitStelem_Any(instruction)) return;
                            break;
                        case Code.Cpblk:
                            if (VisitCpblk(instruction)) return;
                            break;
                        case Code.Initblk:
                            if (VisitInitblk(instruction)) return;
                            break;
                        case Code.Cpobj:
                            if (VisitCpobj(instruction)) return;
                            break;
                        case Code.Stobj:
                            if (VisitStobj(instruction)) return;
                            break;
                        case Code.Stind_I:
                            if (VisitStind_I(instruction)) return;
                            break;
                        case Code.Stind_I1:
                            if (VisitStind_I1(instruction)) return;
                            break;
                        case Code.Stind_I2:
                            if (VisitStind_I2(instruction)) return;
                            break;
                        case Code.Stind_I4:
                            if (VisitStind_I4(instruction)) return;
                            break;
                        case Code.Stind_I8:
                            if (VisitStind_I8(instruction)) return;
                            break;
                        case Code.Stind_R4:
                            if (VisitStind_R4(instruction)) return;
                            break;
                        case Code.Stind_R8:
                            if (VisitStind_R8(instruction)) return;
                            break;
                        case Code.Stind_Ref:
                            if (VisitStind_Ref(instruction)) return;
                            break;
                        case Code.Stsfld:
                            if (VisitStsfld(instruction)) return;
                            break;
                        case Code.Stloc_0:
                            if (VisitStloc_0(instruction)) return;
                            break;
                        case Code.Stloc_1:
                            if (VisitStloc_1(instruction)) return;
                            break;
                        case Code.Stloc_2:
                            if (VisitStloc_2(instruction)) return;
                            break;
                        case Code.Stloc_3:
                            if (VisitStloc_3(instruction)) return;
                            break;
                        case Code.Stloc_S:
                            if (VisitStloc_S(instruction)) return;
                            break;
                        case Code.Stloc:
                            if (VisitStloc(instruction)) return;
                            break;
                        case Code.Stfld:
                            if (VisitStfld(instruction)) return;
                            break;
                        case Code.Add:
                            if (VisitAdd(instruction)) return;
                            break;
                        case Code.Sub:
                            if (VisitSub(instruction)) return;
                            break;
                        case Code.Mul:
                            if (VisitMul(instruction)) return;
                            break;
                        case Code.Div:
                            if (VisitDiv(instruction)) return;
                            break;
                        case Code.Div_Un:
                            if (VisitDiv_Un(instruction)) return;
                            break;
                        case Code.Rem:
                            if (VisitRem(instruction)) return;
                            break;
                        case Code.Rem_Un:
                            if (VisitRem_Un(instruction)) return;
                            break;
                        case Code.Add_Ovf:
                            if (VisitAdd_Ovf(instruction)) return;
                            break;
                        case Code.Add_Ovf_Un:
                            if (VisitAdd_Ovf_Un(instruction)) return;
                            break;
                        case Code.Mul_Ovf:
                            if (VisitMul_Ovf(instruction)) return;
                            break;
                        case Code.Mul_Ovf_Un:
                            if (VisitMul_Ovf_Un(instruction)) return;
                            break;
                        case Code.Sub_Ovf:
                            if (VisitSub_Ovf(instruction)) return;
                            break;
                        case Code.Sub_Ovf_Un:
                            if (VisitSub_Ovf_Un(instruction)) return;
                            break;
                        case Code.And:
                            if (VisitAnd(instruction)) return;
                            break;
                        case Code.Or:
                            if (VisitOr(instruction)) return;
                            break;
                        case Code.Xor:
                            if (VisitXor(instruction)) return;
                            break;
                        case Code.Shl:
                            if (VisitShl(instruction)) return;
                            break;
                        case Code.Shr:
                            if (VisitShr(instruction)) return;
                            break;
                        case Code.Shr_Un:
                            if (VisitShr_Un(instruction)) return;
                            break;
                        case Code.Ceq:
                            if (VisitCeq(instruction)) return;
                            break;
                        case Code.Cgt:
                            if (VisitCgt(instruction)) return;
                            break;
                        case Code.Cgt_Un:
                            if (VisitCgt_Un(instruction)) return;
                            break;
                        case Code.Clt:
                            if (VisitClt(instruction)) return;
                            break;
                        case Code.Clt_Un:
                            if (VisitClt_Un(instruction)) return;
                            break;
                        case Code.Ldelem_I1:
                            if (VisitLdelem_I1(instruction)) return;
                            break;
                        case Code.Ldelem_U1:
                            if (VisitLdelem_U1(instruction)) return;
                            break;
                        case Code.Ldelem_I2:
                            if (VisitLdelem_I2(instruction)) return;
                            break;
                        case Code.Ldelem_U2:
                            if (VisitLdelem_U2(instruction)) return;
                            break;
                        case Code.Ldelem_I4:
                            if (VisitLdelem_I4(instruction)) return;
                            break;
                        case Code.Ldelem_U4:
                            if (VisitLdelem_U4(instruction)) return;
                            break;
                        case Code.Ldelem_I8:
                            if (VisitLdelem_I8(instruction)) return;
                            break;
                        case Code.Ldelem_I:
                            if (VisitLdelem_I(instruction)) return;
                            break;
                        case Code.Ldelem_R4:
                            if (VisitLdelem_R4(instruction)) return;
                            break;
                        case Code.Ldelem_R8:
                            if (VisitLdelem_R8(instruction)) return;
                            break;
                        case Code.Ldelem_Ref:
                            if (VisitLdelem_Ref(instruction)) return;
                            break;
                        case Code.Ldelem_Any:
                            if (VisitLdelem_Any(instruction)) return;
                            break;
                        case Code.Ldelema:
                            if (VisitLdelema(instruction)) return;
                            break;
                        case Code.Starg:
                            if (VisitStarg(instruction)) return;
                            break;
                        case Code.Starg_S:
                            if (VisitStarg_S(instruction)) return;
                            break;
                        case Code.Initobj:
                            if (VisitInitobj(instruction)) return;
                            break;
                        case Code.Pop:
                            if (VisitPop(instruction)) return;
                            break;
                        case Code.Nop:
                            if (VisitNop(instruction)) return;
                            break;
                        case Code.Break:
                            if (VisitBreak(instruction)) return;
                            break;
                        case Code.Readonly:
                            if (VisitReadonly(instruction)) return;
                            break;
                        case Code.Neg:
                            if (VisitNeg(instruction)) return;
                            break;
                        case Code.Not:
                            if (VisitNot(instruction)) return;
                            break;
                        case Code.Conv_I:
                            if (VisitConv_I(instruction)) return;
                            break;
                        case Code.Conv_I1:
                            if (VisitConv_I1(instruction)) return;
                            break;
                        case Code.Conv_I2:
                            if (VisitConv_I2(instruction)) return;
                            break;
                        case Code.Conv_I4:
                            if (VisitConv_I4(instruction)) return;
                            break;
                        case Code.Conv_I8:
                            if (VisitConv_I8(instruction)) return;
                            break;
                        case Code.Conv_R4:
                            if (VisitConv_R4(instruction)) return;
                            break;
                        case Code.Conv_R8:
                            if (VisitConv_R8(instruction)) return;
                            break;
                        case Code.Conv_U:
                            if (VisitConv_U(instruction)) return;
                            break;
                        case Code.Conv_U1:
                            if (VisitConv_U1(instruction)) return;
                            break;
                        case Code.Conv_U2:
                            if (VisitConv_U2(instruction)) return;
                            break;
                        case Code.Conv_U4:
                            if (VisitConv_U4(instruction)) return;
                            break;
                        case Code.Conv_U8:
                            if (VisitConv_U8(instruction)) return;
                            break;
                        case Code.Conv_R_Un:
                            if (VisitConv_R_Un(instruction)) return;
                            break;
                        case Code.Conv_Ovf_I:
                            if (VisitConv_Ovf_I(instruction)) return;
                            break;
                        case Code.Conv_Ovf_I1:
                            if (VisitConv_Ovf_I1(instruction)) return;
                            break;
                        case Code.Conv_Ovf_I2:
                            if (VisitConv_Ovf_I2(instruction)) return;
                            break;
                        case Code.Conv_Ovf_I4:
                            if (VisitConv_Ovf_I4(instruction)) return;
                            break;
                        case Code.Conv_Ovf_I8:
                            if (VisitConv_Ovf_I8(instruction)) return;
                            break;
                        case Code.Conv_Ovf_I_Un:
                            if (VisitConv_Ovf_I_Un(instruction)) return;
                            break;
                        case Code.Conv_Ovf_I1_Un:
                            if (VisitConv_Ovf_I1_Un(instruction)) return;
                            break;
                        case Code.Conv_Ovf_I2_Un:
                            if (VisitConv_Ovf_I2_Un(instruction)) return;
                            break;
                        case Code.Conv_Ovf_I4_Un:
                            if (VisitConv_Ovf_I4_Un(instruction)) return;
                            break;
                        case Code.Conv_Ovf_I8_Un:
                            if (VisitConv_Ovf_I8_Un(instruction)) return;
                            break;
                        case Code.Conv_Ovf_U:
                            if (VisitConv_Ovf_U(instruction)) return;
                            break;
                        case Code.Conv_Ovf_U1:
                            if (VisitConv_Ovf_U1(instruction)) return;
                            break;
                        case Code.Conv_Ovf_U2:
                            if (VisitConv_Ovf_U2(instruction)) return;
                            break;
                        case Code.Conv_Ovf_U4:
                            if (VisitConv_Ovf_U4(instruction)) return;
                            break;
                        case Code.Conv_Ovf_U8:
                            if (VisitConv_Ovf_U8(instruction)) return;
                            break;
                        case Code.Conv_Ovf_U_Un:
                            if (VisitConv_Ovf_U_Un(instruction)) return;
                            break;
                        case Code.Conv_Ovf_U1_Un:
                            if (VisitConv_Ovf_U1_Un(instruction)) return;
                            break;
                        case Code.Conv_Ovf_U2_Un:
                            if (VisitConv_Ovf_U2_Un(instruction)) return;
                            break;
                        case Code.Conv_Ovf_U4_Un:
                            if (VisitConv_Ovf_U4_Un(instruction)) return;
                            break;
                        case Code.Conv_Ovf_U8_Un:
                            if (VisitConv_Ovf_U8_Un(instruction)) return;
                            break;
                        case Code.Ldind_I1:
                            if (VisitLdind_I1(instruction)) return;
                            break;
                        case Code.Ldind_U1:
                            if (VisitLdind_U1(instruction)) return;
                            break;
                        case Code.Ldind_I2:
                            if (VisitLdind_I2(instruction)) return;
                            break;
                        case Code.Ldind_U2:
                            if (VisitLdind_U2(instruction)) return;
                            break;
                        case Code.Ldind_I4:
                            if (VisitLdind_I4(instruction)) return;
                            break;
                        case Code.Ldind_U4:
                            if (VisitLdind_U4(instruction)) return;
                            break;
                        case Code.Ldind_I8:
                            if (VisitLdind_I8(instruction)) return;
                            break;
                        case Code.Ldind_I:
                            if (VisitLdind_I(instruction)) return;
                            break;
                        case Code.Ldind_R4:
                            if (VisitLdind_R4(instruction)) return;
                            break;
                        case Code.Ldind_R8:
                            if (VisitLdind_R8(instruction)) return;
                            break;
                        case Code.Ldind_Ref:
                            if (VisitLdind_Ref(instruction)) return;
                            break;
                        case Code.Castclass:
                            if (VisitCastclass(instruction)) return;
                            break;
                        case Code.Isinst:
                            if (VisitIsinst(instruction)) return;
                            break;
                        case Code.Unbox:
                            if (VisitUnbox(instruction)) return;
                            break;
                        case Code.Unbox_Any:
                            if (VisitUnbox_Any(instruction)) return;
                            break;
                        case Code.Box:
                            if (VisitBox(instruction)) return;
                            break;
                        case Code.Ldobj:
                            if (VisitLdobj(instruction)) return;
                            break;
                        case Code.Ldfld:
                            if (VisitLdfld(instruction)) return;
                            break;
                        case Code.Ldflda:
                            if (VisitLdflda(instruction)) return;
                            break;
                        case Code.Ldlen:
                            if (VisitLdlen(instruction)) return;
                            break;
                        case Code.Newarr:
                            if (VisitNewarr(instruction)) return;
                            break;
                        case Code.Refanyval:
                            if (VisitRefanyval(instruction)) return;
                            break;
                        case Code.Ckfinite:
                            if (VisitCkfinite(instruction)) return;
                            break;
                        case Code.Mkrefany:
                            if (VisitMkrefany(instruction)) return;
                            break;
                        case Code.Refanytype:
                            if (VisitRefanytype(instruction)) return;
                            break;
                        case Code.Localloc:
                            if (VisitLocalloc(instruction)) return;
                            break;
                        case Code.Unaligned:
                            if (VisitUnaligned(instruction)) return;
                            break;
                        case Code.Volatile:
                            if (VisitVolatile(instruction)) return;
                            break;
                        case Code.Tail:
                            if (VisitTail(instruction)) return;
                            break;
                        case Code.Constrained:
                            if (VisitConstrained(instruction)) return;
                            break;
                        default:
                            throw new InvalidOperationException("Unrecognized opCode.");
                    }
                } while ((instruction = instruction.Next) != null);
            }
            catch (FodyWeavingException ex)
            {
                if (ex.InnerException == null) ex.Instruction = instruction;
                throw;
            }
            catch (Exception ex)
            {
                throw new FodyWeavingException(ex.Message, instruction);
            }
        }

        protected virtual bool VisitNo(Instruction instruction)
        {
            throw new InvalidOperationException("Please share your assembly with me; there is an instruction 'no.' that I have never encountered before");
        }

        protected virtual bool VisitJmp(Instruction instruction)
        {
            return true;
        }

        protected virtual bool VisitRet(Instruction instruction)
        {
            if (!_isVoid) _stackDepth--;

            return true;
        }

        protected virtual bool VisitEndfilter(Instruction instruction)
        {
            _stackDepth--;
            return true;
        }

        protected virtual bool VisitThrow(Instruction instruction)
        {
            _stackDepth.Reset();
            return true;
        }

        protected virtual bool VisitRethrow(Instruction instruction)
        {
            return true;
        }

        protected virtual bool VisitEndfinally(Instruction instruction)
        {
            return true;
        }

        protected virtual bool VisitLeave(Instruction instruction)
        {
            Visit((Instruction)instruction.Operand);

            return true;
        }

        protected virtual bool VisitLeave_S(Instruction instruction)
        {
            Visit((Instruction)instruction.Operand);

            return true;
        }

        protected virtual bool VisitBr_S(Instruction instruction)
        {
            Visit((Instruction)instruction.Operand);

            return true;
        }

        protected virtual bool VisitBr(Instruction instruction)
        {
            Visit((Instruction)instruction.Operand);

            return true;
        }

        protected virtual bool VisitSwitch(Instruction instruction)
        {
            _stackDepth--;

            var instructions = (Instruction[])instruction.Operand;

            foreach (var to in instructions)
            {
                using var _ = _stackDepth.Stash();
                using var __ = _visitStack.Stash();

                Visit(to);
            }

            return false;
        }

        protected virtual bool VisitBrfalse_S(Instruction instruction)
        {
            _stackDepth--;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();

            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBrtrue_S(Instruction instruction)
        {
            _stackDepth--;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();

            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBrfalse(Instruction instruction)
        {
            _stackDepth--;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();

            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBrtrue(Instruction instruction)
        {
            _stackDepth--;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();

            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBeq_S(Instruction instruction)
        {
            _stackDepth -= 2;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();
            
            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBge_S(Instruction instruction)
        {
            _stackDepth -= 2;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();
            
            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBgt_S(Instruction instruction)
        {
            _stackDepth -= 2;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();
            
            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBle_S(Instruction instruction)
        {
            _stackDepth -= 2;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();
            
            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBlt_S(Instruction instruction)
        {
            _stackDepth -= 2;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();
            
            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBne_Un_S(Instruction instruction)
        {
            _stackDepth -= 2;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();
            
            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBge_Un_S(Instruction instruction)
        {
            _stackDepth -= 2;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();
            
            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBgt_Un_S(Instruction instruction)
        {
            _stackDepth -= 2;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();
            
            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBle_Un_S(Instruction instruction)
        {
            _stackDepth -= 2;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();
            
            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBlt_Un_S(Instruction instruction)
        {
            _stackDepth -= 2;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();
            
            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBeq(Instruction instruction)
        {
            _stackDepth -= 2;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();
            
            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBge(Instruction instruction)
        {
            _stackDepth -= 2;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();
            
            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBgt(Instruction instruction)
        {
            _stackDepth -= 2;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();
            
            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBle(Instruction instruction)
        {
            _stackDepth -= 2;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();
            
            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBlt(Instruction instruction)
        {
            _stackDepth -= 2;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();
            
            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBne_Un(Instruction instruction)
        {
            _stackDepth -= 2;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();
            
            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBge_Un(Instruction instruction)
        {
            _stackDepth -= 2;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();
            
            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBgt_Un(Instruction instruction)
        {
            _stackDepth -= 2;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();
            
            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBle_Un(Instruction instruction)
        {
            _stackDepth -= 2;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();
            
            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitBlt_Un(Instruction instruction)
        {
            _stackDepth -= 2;

            using var _ = _stackDepth.Stash();
            using var __ = _visitStack.Stash();
            
            Visit((Instruction)instruction.Operand);

            return false;
        }

        protected virtual bool VisitNewobj(Instruction instruction)
        {
            var methodRef = (MethodReference)instruction.Operand;
            _stackDepth -= methodRef.Parameters.Count;
            _stackDepth++;

            return false;
        }

        protected virtual bool VisitCall(Instruction instruction)
        {
            var methodRef = (MethodReference)instruction.Operand;
            if (methodRef.HasThis) _stackDepth--;

            _stackDepth -= methodRef.Parameters.Count;

            if (!methodRef.ReturnType.StrictIsVoid()) _stackDepth++;

            return false;
        }

        protected virtual bool VisitCallvirt(Instruction instruction)
        {
            var methodRef = (MethodReference)instruction.Operand;
            if (methodRef.HasThis) _stackDepth--;

            _stackDepth -= methodRef.Parameters.Count;

            if (!methodRef.ReturnType.StrictIsVoid()) _stackDepth++;

            return false;
        }

        protected virtual bool VisitCalli(Instruction instruction)
        {
            var callSite = (CallSite)instruction.Operand;
            _stackDepth -= callSite.Parameters.Count + 1;
            if (!callSite.ReturnType.StrictIsVoid()) _stackDepth++;

            return false;
        }

        protected virtual bool VisitLdarg(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdarg_S(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdarg_0(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdarg_1(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdarg_2(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdarg_3(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdarga(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdarga_S(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdloca(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdloca_S(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdloc(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdloc_S(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdloc_0(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdloc_1(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdloc_2(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdloc_3(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdc_I4_M1(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdc_I4_0(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdc_I4_1(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdc_I4_2(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdc_I4_3(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdc_I4_4(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdc_I4_5(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdc_I4_6(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdc_I4_7(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdc_I4_8(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdc_I4_S(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdc_I4(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdc_I8(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdc_R4(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdc_R8(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitDup(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdnull(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdstr(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdsfld(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdsflda(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdtoken(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdftn(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitArglist(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitLdvirtftn(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitSizeof(Instruction instruction)
        {
            _stackDepth++;
            return false;
        }

        protected virtual bool VisitStelem_I(Instruction instruction)
        {
            _stackDepth -= 3;
            return false;
        }

        protected virtual bool VisitStelem_I1(Instruction instruction)
        {
            _stackDepth -= 3;
            return false;
        }

        protected virtual bool VisitStelem_I2(Instruction instruction)
        {
            _stackDepth -= 3;
            return false;
        }

        protected virtual bool VisitStelem_I4(Instruction instruction)
        {
            _stackDepth -= 3;
            return false;
        }

        protected virtual bool VisitStelem_I8(Instruction instruction)
        {
            _stackDepth -= 3;
            return false;
        }

        protected virtual bool VisitStelem_R4(Instruction instruction)
        {
            _stackDepth -= 3;
            return false;
        }

        protected virtual bool VisitStelem_R8(Instruction instruction)
        {
            _stackDepth -= 3;
            return false;
        }

        protected virtual bool VisitStelem_Ref(Instruction instruction)
        {
            _stackDepth -= 3;
            return false;
        }

        protected virtual bool VisitStelem_Any(Instruction instruction)
        {
            _stackDepth -= 3;
            return false;
        }

        protected virtual bool VisitCpblk(Instruction instruction)
        {
            _stackDepth -= 3;
            return false;
        }

        protected virtual bool VisitInitblk(Instruction instruction)
        {
            _stackDepth -= 3;
            return false;
        }

        protected virtual bool VisitCpobj(Instruction instruction)
        {
            _stackDepth -= 2;
            return false;
        }

        protected virtual bool VisitStobj(Instruction instruction)
        {
            _stackDepth -= 2;
            return false;
        }

        protected virtual bool VisitStind_I(Instruction instruction)
        {
            _stackDepth -= 2;
            return false;
        }

        protected virtual bool VisitStind_I1(Instruction instruction)
        {
            _stackDepth -= 2;
            return false;
        }

        protected virtual bool VisitStind_I2(Instruction instruction)
        {
            _stackDepth -= 2;
            return false;
        }

        protected virtual bool VisitStind_I4(Instruction instruction)
        {
            _stackDepth -= 2;
            return false;
        }

        protected virtual bool VisitStind_I8(Instruction instruction)
        {
            _stackDepth -= 2;
            return false;
        }

        protected virtual bool VisitStind_R4(Instruction instruction)
        {
            _stackDepth -= 2;
            return false;
        }

        protected virtual bool VisitStind_R8(Instruction instruction)
        {
            _stackDepth -= 2;
            return false;
        }

        protected virtual bool VisitStind_Ref(Instruction instruction)
        {
            _stackDepth -= 2;
            return false;
        }

        protected virtual bool VisitStfld(Instruction instruction)
        {
            _stackDepth -= 2;
            return false;
        }

        protected virtual bool VisitStsfld(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitStloc_0(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitStloc_1(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitStloc_2(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitStloc_3(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitStloc_S(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitStloc(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitAdd(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitSub(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitMul(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitDiv(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitDiv_Un(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitRem(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitRem_Un(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitAdd_Ovf(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitAdd_Ovf_Un(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitMul_Ovf(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitMul_Ovf_Un(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitSub_Ovf(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitSub_Ovf_Un(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitAnd(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitOr(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitXor(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitShl(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitShr(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitShr_Un(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitCeq(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitCgt(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitCgt_Un(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitClt(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitClt_Un(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitLdelem_I1(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitLdelem_U1(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitLdelem_I2(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitLdelem_U2(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitLdelem_I4(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitLdelem_U4(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitLdelem_I8(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitLdelem_I(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitLdelem_R4(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitLdelem_R8(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitLdelem_Ref(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitLdelem_Any(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitLdelema(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitStarg(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitStarg_S(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitInitobj(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitPop(Instruction instruction)
        {
            _stackDepth--;
            return false;
        }

        protected virtual bool VisitNop(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitBreak(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitReadonly(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitNeg(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitNot(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_I(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_I1(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_I2(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_I4(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_I8(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_R4(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_R8(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_U(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_U1(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_U2(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_U4(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_U8(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_R_Un(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_Ovf_I(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_Ovf_I1(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_Ovf_I2(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_Ovf_I4(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_Ovf_I8(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_Ovf_I_Un(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_Ovf_I1_Un(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_Ovf_I2_Un(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_Ovf_I4_Un(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_Ovf_I8_Un(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_Ovf_U(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_Ovf_U1(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_Ovf_U2(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_Ovf_U4(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_Ovf_U8(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_Ovf_U_Un(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_Ovf_U1_Un(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_Ovf_U2_Un(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_Ovf_U4_Un(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConv_Ovf_U8_Un(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitLdind_I1(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitLdind_U1(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitLdind_I2(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitLdind_U2(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitLdind_I4(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitLdind_U4(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitLdind_I8(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitLdind_I(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitLdind_R4(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitLdind_R8(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitLdind_Ref(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitCastclass(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitIsinst(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitUnbox(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitUnbox_Any(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitBox(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitLdobj(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitLdfld(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitLdflda(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitLdlen(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitNewarr(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitRefanyval(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitCkfinite(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitMkrefany(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitRefanytype(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitLocalloc(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitUnaligned(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitVolatile(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitTail(Instruction instruction)
        {
            return false;
        }

        protected virtual bool VisitConstrained(Instruction instruction)
        {
            return false;
        }
    }
}
