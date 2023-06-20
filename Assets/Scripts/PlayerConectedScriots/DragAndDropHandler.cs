using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DragAndDropHandler : MonoBehaviour {

    [SerializeField] private UIItemSlot cursorSlot = null;
    private ItemSlot cursorItemSlot;

    [SerializeField] private GraphicRaycaster m_Raycaster = null;
    private PointerEventData m_PointerEventData;
    [SerializeField] private EventSystem m_EventSystem = null;

    World world;

    private void Start() {

        world = GameObject.Find("World").GetComponent<World>();

        cursorItemSlot = new ItemSlot(cursorSlot);

    }

    private void Update() {

        if (!world.inUI)
            return;

        cursorSlot.transform.position = Input.mousePosition;

        if (Input.GetMouseButtonDown(0)) {

            HandleSlotClickLeft(CheckForSlot());

        }
        if (Input.GetMouseButtonDown(1)) {

            HandleSlotClickRight(CheckForSlot());
        }
    }

    private void HandleSlotClickLeft (UIItemSlot clickedSlot) {

        if (!cursorSlot.HasItem && !clickedSlot.HasItem)
            return;

        if(!cursorSlot.HasItem && clickedSlot == null)
            return;

        if(cursorSlot.HasItem && clickedSlot == null)
        {
            cursorItemSlot.EmptySlot();
            cursorSlot.UpdateSlot();
            return;
        }

        if (clickedSlot.itemSlot.isCreative) {

            cursorItemSlot.EmptySlot();
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.stack);
        }

        if (!cursorSlot.HasItem && clickedSlot.HasItem) {

            cursorItemSlot.InsertStack(clickedSlot.itemSlot.TakeAll());
            return;

        }

        if (cursorSlot.HasItem && !clickedSlot.HasItem) {

            clickedSlot.itemSlot.InsertStack(cursorItemSlot.TakeAll());
            return;

        }

        if (cursorSlot.HasItem && clickedSlot.HasItem) {

            if (cursorSlot.itemSlot.stack.id != clickedSlot.itemSlot.stack.id) {

                ItemStack oldCursorSlot = cursorSlot.itemSlot.TakeAll();
                ItemStack oldSlot = clickedSlot.itemSlot.TakeAll();

                clickedSlot.itemSlot.InsertStack(oldCursorSlot);
                cursorSlot.itemSlot.InsertStack(oldSlot);

            }
        }

        if(cursorSlot.HasItem && clickedSlot.HasItem)
        {
            if(cursorSlot.itemSlot.stack.id == clickedSlot.itemSlot.stack.id)
            {
                if(cursorSlot.itemSlot.stack.amount > clickedSlot.itemSlot.stack.amount)
                {
                    int container = cursorSlot.itemSlot.stack.amount - clickedSlot.itemSlot.stack.amount;

                    clickedSlot.itemSlot.stack.amount += container;
                    cursorSlot.itemSlot.stack.amount -= container;
                    clickedSlot.UpdateSlot();
                    cursorSlot.UpdateSlot();
                }

                else if(cursorSlot.itemSlot.stack.amount < clickedSlot.itemSlot.stack.amount)
                {
                    int container = clickedSlot.itemSlot.stack.amount - cursorSlot.itemSlot.stack.amount;

                    clickedSlot.itemSlot.stack.amount -= container;
                    cursorSlot.itemSlot.stack.amount += container;
                    cursorSlot.UpdateSlot();
                    clickedSlot.UpdateSlot();
                }

                else if(cursorSlot.itemSlot.stack.amount == clickedSlot.itemSlot.stack.amount)
                {
                    if(cursorSlot.itemSlot.stack.amount > 32)
                    {
                    int container = clickedSlot.itemSlot.stack.amount - cursorSlot.itemSlot.stack.amount;

                    clickedSlot.itemSlot.stack.amount -= container;
                    cursorSlot.itemSlot.stack.amount += container;
                    cursorSlot.UpdateSlot();
                    clickedSlot.UpdateSlot();
                    }
                    else
                    {
                        clickedSlot.itemSlot.stack.amount += cursorSlot.itemSlot.stack.amount;
                        cursorSlot.UpdateSlot();
                        clickedSlot.UpdateSlot();
                    }
                }
            }
        }
    }


    private void HandleSlotClickRight(UIItemSlot clickedSlot)
    {
        if (!cursorSlot.HasItem && !clickedSlot.HasItem)
            return;

        if(cursorSlot.HasItem)
        {
            if(clickedSlot == null)
            {
                cursorSlot.itemSlot.stack.amount--;
                cursorSlot.UpdateSlot();
            }
            else
            {
                if(cursorSlot.HasItem && clickedSlot.HasItem)
                {
                    if(cursorSlot.itemSlot.stack.id == clickedSlot.itemSlot.stack.id)
                    {
                        if(clickedSlot.itemSlot.stack.amount < 64)
                        {
                            clickedSlot.itemSlot.stack.amount++;
                            clickedSlot.UpdateSlot();
                            cursorSlot.itemSlot.stack.amount--;
                            cursorSlot.UpdateSlot();
                        }
                        else
                            return;
                    }
                    else
                        return;
                }
            }
        }
    }

    private UIItemSlot CheckForSlot () {

        m_PointerEventData = new PointerEventData(m_EventSystem);
        m_PointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        m_Raycaster.Raycast(m_PointerEventData, results);

        foreach (RaycastResult result in results) {

            if (result.gameObject.tag == "UIItemSlot")
                return result.gameObject.GetComponent<UIItemSlot>();

        }

        return null;

    }

}
