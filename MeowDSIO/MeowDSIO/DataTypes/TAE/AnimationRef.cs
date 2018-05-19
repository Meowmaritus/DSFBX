using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.TAE
{
    public class AnimationRef : Data
    {
        //Values below 0 aren't valid in-game so the GUI can easily warn of an ID that hasn't been set.
        public int ID { get; set; } = -1;
        public Animation Anim { get; set; } = new Animation();

        public AnimationRef() { }

        public AnimationRef(int ID)
        {
            this.ID = ID;
            ResetToDefaultFileName();
        }

        public void AddNewEvent()
        {
            var newEvent = new AnimationEvent(Anim.Events.Count + 1, AnimationEventType.ApplySpecialProperty, ID);
            Anim.Events.Add(newEvent);
        }

        public void ResetToDefaultFileName()
        {
            int leftNum = 00;
            int rightNum = 0000;

            if (ID >= 01_0000)
            {
                leftNum = ID / 01_0000;
                rightNum = ID % 01_0000;
            }
            else
            {
                rightNum = ID;
            }

            Anim.FileName = $"a{leftNum:D02}_{rightNum:D04}.HKXwin";
        }
    }
}
