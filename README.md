# UGUIScrollPageView
reuse cell &amp; page mode

TODO: 10.page模式下对ScrollBar的支持 12.考虑分离横纵两种模式的代码，从而简化此文件 13.content bounds 14.独立Bound检测功能  
DONE: 1.配合tween实现的PageMode,定位。(OnEndDrag)   
      2.对外接口。(init,refresh,scrollto)   
      3.同一列表的复用（重新Init后，对cells进行增删或重置)   
      4.cell的加载(外部加载模板后注入)   
      5.规范锚点(左上角)  
      6.Editor自动检查组件完整性(特例不少，过于繁杂，直接提供模板prefab)   
      7.确定数据传入方式(_onRefresh, _onPageIndexChanged)   
      8.Page切换   
      9.page模式下对滚轮的支持   
 	11.视需求决定 cellsize的是简单的自动获取还是同grid一样硬性配置(增加Spacing,cellSize自动获取)  
